using UnityEngine;
using System.Collections.Generic;
using ArrowBlast.Core;
using DG.Tweening;
using System;

namespace ArrowBlast.Game
{
    /// <summary>
    /// 3D Arrow with MeshRenderer
    /// Moves with 2D-style animation when collected
    /// </summary>
    public class Arrow : MonoBehaviour
    {
        public BlockColor Color { get; private set; }
        public Direction ArrowDirection { get; private set; }
        public int Length { get; private set; }
        public float CellSize { get; private set; } = 0.8f;

        public int GridX { get; private set; }
        public int GridY { get; private set; }
        public List<Vector2Int> Segments { get; private set; } = new List<Vector2Int>();

        [Header("Materials")]
        [SerializeField] private Material headMaterial;
        [SerializeField] private Material bodyMaterial;
        [SerializeField] private Sprite arrowIconSprite;

        private GameObject headObject;
        private GameObject iconObject;
        private List<GameObject> bodyParts = new List<GameObject>(); // Individual body segments for animation

        private Tween shakeTween;
        private Vector3 originalLocalPos;
        private MaterialPropertyBlock _propBlock;

        public void SetScared(bool isScared)
        {
            if (isScared)
            {
                if (shakeTween == null || !shakeTween.IsActive())
                {
                    originalLocalPos = transform.localPosition;
                    shakeTween = transform.DOShakePosition(100f, 0.02f, 5, 90, false, false)
                        .SetLoops(-1, LoopType.Restart)
                        .SetEase(Ease.Linear);
                }
            }
            else
            {
                if (shakeTween != null)
                {
                    if (shakeTween.IsActive()) shakeTween.Kill();
                    shakeTween = null;
                    transform.localPosition = originalLocalPos;
                }
            }
        }

        public void Init(BlockColor color, Direction dir, int length, int x, int y, List<Vector2Int> segments = null, float cellSize = 0.8f)
        {
            Color = color;
            ArrowDirection = dir;
            Length = length;
            GridX = x;
            GridY = y;
            CellSize = cellSize;

            if (segments != null && segments.Count > 0)
            {
                Segments = new List<Vector2Int>(segments);
            }
            else
            {
                // Generate linear segments if none provided
                Segments = GenerateLinearSegments();
            }

            UpdateVisuals();
            CreateProceduralVisuals();

            // Ensure collider is present and enabled
            BoxCollider col = GetComponent<BoxCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider>();
            }

            // Adjust collider to cover all segments
            FitColliderToSegments(col);
            col.enabled = true;
            // Debug.Log($"[ARROW INIT] Arrow initialized at ({GridX}, {GridY}), Length: {Length}");
        }

        public int GetAmmoAmount()
        {
            switch (Length)
            {
                case 1: return 10;
                case 2: return 20;
                case 3: return 20;
                case 4: return 40;
                default: return 10;
            }
        }

        private Color GetArrowColor()
        {
            return GamePalette.GetColor(Color);
        }

        public List<Vector2Int> GetOccupiedCells()
        {
            return new List<Vector2Int>(Segments);
        }

        private List<Vector2Int> GenerateLinearSegments()
        {
            var cells = new List<Vector2Int>();
            Vector2Int head = new Vector2Int(GridX, GridY);
            cells.Add(head);

            Vector2Int back = Vector2Int.zero;
            switch (ArrowDirection)
            {
                case Direction.Up: back = new Vector2Int(0, -1); break;
                case Direction.Down: back = new Vector2Int(0, 1); break;
                case Direction.Left: back = new Vector2Int(1, 0); break;
                case Direction.Right: back = new Vector2Int(-1, 0); break;
            }

            for (int i = 1; i < Length; i++)
            {
                cells.Add(head + back * i);
            }
            return cells;
        }



        private void CreateProceduralVisuals()
        {
            // 1. Cleanup ALL children
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
            bodyParts.Clear();

            if (Segments == null || Segments.Count == 0) return;

            Color arrowColor = GetArrowColor();
            // Set scale to 1.0f so segments connect perfectly without gaps
            float visualScale = 1.0f;

            // 2. Create Head (at Segments[0])
            headObject = new GameObject("Head");
            headObject.transform.SetParent(transform);
            headObject.transform.localPosition = Vector3.zero;
            headObject.transform.localScale = Vector3.one * visualScale * CellSize;

            // Visual rotation for the head to show direction
            float rotZ = 0;
            switch (ArrowDirection)
            {
                case Direction.Up: rotZ = 0; break;
                case Direction.Right: rotZ = -90; break;
                case Direction.Down: rotZ = 180; break;
                case Direction.Left: rotZ = 90; break;
            }
            headObject.transform.localRotation = Quaternion.Euler(0, 0, rotZ);

            MeshFilter hMf = headObject.AddComponent<MeshFilter>();
            MeshRenderer hMr = headObject.AddComponent<MeshRenderer>();
            RoundedCube hRc = headObject.AddComponent<RoundedCube>();
            hRc.xSize = 5; hRc.ySize = 5; hRc.zSize = 5; // Resolution
            hRc.roundness = 0.15f; // Soft beveled look
            hRc.Generate();

            hMr.sharedMaterial = headMaterial != null ? headMaterial : new Material(Shader.Find("Standard"));

            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
            hMr.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_Color", arrowColor);
            _propBlock.SetColor("_BaseColor", arrowColor);
            hMr.SetPropertyBlock(_propBlock);

            // Add Sprite Icon on top
            if (arrowIconSprite != null)
            {
                iconObject = new GameObject("Icon");
                iconObject.transform.SetParent(headObject.transform);
                iconObject.transform.localPosition = new Vector3(0, 0, -0.6f);
                iconObject.transform.localScale = Vector3.one * 0.3f;
                iconObject.transform.localRotation = Quaternion.identity;

                SpriteRenderer sr = iconObject.AddComponent<SpriteRenderer>();
                sr.sprite = arrowIconSprite;
                sr.color = UnityEngine.Color.black;
            }

            // 3. Create Body Parts (Remaining segments)
            for (int i = 1; i < Segments.Count; i++)
            {
                Vector2Int cell = Segments[i];
                Vector2Int head = Segments[0];

                GameObject bodyPart = new GameObject($"BodyPart_{i}");
                bodyPart.transform.SetParent(transform);

                // Position relative to head in grid space
                bodyPart.transform.localPosition = new Vector3(
                    (cell.x - head.x) * CellSize,
                    (cell.y - head.y) * CellSize,
                    0
                );

                bodyPart.transform.localScale = Vector3.one * visualScale * CellSize;
                bodyPart.transform.localRotation = Quaternion.identity;

                MeshFilter bMf = bodyPart.AddComponent<MeshFilter>();
                MeshRenderer bMr = bodyPart.AddComponent<MeshRenderer>();
                RoundedCube bRc = bodyPart.AddComponent<RoundedCube>();
                bRc.xSize = 5; bRc.ySize = 5; bRc.zSize = 5;
                bRc.roundness = 0.15f;
                bRc.Generate();

                bMr.sharedMaterial = bodyMaterial != null ? bodyMaterial : new Material(Shader.Find("Standard"));
                if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
                bMr.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_Color", arrowColor);
                _propBlock.SetColor("_BaseColor", arrowColor);
                bMr.SetPropertyBlock(_propBlock);

                bodyParts.Add(bodyPart);
            }
        }

        private void FitColliderToSegments(BoxCollider col)
        {
            if (Segments == null || Segments.Count == 0) return;

            // Calculate bounds in local space relative to root (Head)
            Vector3 min = Vector3.zero;
            Vector3 max = Vector3.zero;

            foreach (var seg in Segments)
            {
                Vector3 localPos = new Vector3(
                    (seg.x - GridX) * CellSize,
                    (seg.y - GridY) * CellSize,
                    0
                );
                min = Vector3.Min(min, localPos);
                max = Vector3.Max(max, localPos);
            }

            col.center = (min + max) / 2f;
            // Pad by cellSize to cover the whole cube
            col.size = (max - min) + new Vector3(CellSize * 0.9f, CellSize * 0.9f, CellSize * 0.5f);
        }

        /// <summary>
        /// Animate arrow moving from bottom grid to middle slot position (2D-style)
        /// </summary>
        public void AnimateToSlot(Vector3 targetPosition, System.Action onComplete = null)
        {
            StartCoroutine(MoveToSlotCoroutine(targetPosition, onComplete));
        }

        private System.Collections.IEnumerator MoveToSlotCoroutine(Vector3 target, System.Action onComplete)
        {
            Vector3 start = transform.position;
            float duration = 0.5f;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Smooth ease out
                t = 1f - Mathf.Pow(1f - t, 2f);

                // Move in 2D plane (XY only, Z stays 0)
                Vector3 currentPos = Vector3.Lerp(start, target, t);
                currentPos.z = 0;
                transform.position = currentPos;

                yield return null;
            }

            transform.position = target;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Visual feedback when the arrow is clicked but blocked
        /// </summary>
        public void AnimateBlocked()
        {
            // Shake the arrow slightly or pulse it
            transform.DOComplete(); // Stop any current animation
            transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 10, 1)
                .SetEase(Ease.OutQuad);

            // Optional: Shake position slightly in the direction it's blocked
            Vector3 shakeDir = Vector3.zero;
            switch (ArrowDirection)
            {
                case Direction.Up: shakeDir = Vector3.up; break;
                case Direction.Down: shakeDir = Vector3.down; break;
                case Direction.Left: shakeDir = Vector3.left; break;
                case Direction.Right: shakeDir = Vector3.right; break;
            }
            transform.DOPunchPosition(shakeDir * 0.1f, 0.3f, 10, 1);
        }

        /// <summary>
        /// DOTween collection animation:
        /// 1. Move in arrow direction until head exits grid
        /// 2. Stop head, continue body parts merging
        /// 3. Call ammo increment callback for each merge
        /// 4. Move to target slot position
        /// </summary>
        public void AnimateCollection(Vector3 exitDirection, Vector3 exitTargetPosition, Vector3 slotPosition,
            Action onHeadArrived, Action<int> onAmmoIncrement, Action onComplete)
        {
            Sequence sequence = DOTween.Sequence();

            float moveDuration = 0.5f;
            float headMoveToSlotDuration = 0.4f;
            float bodyMergeDuration = 0.3f;

            // Debug.Log($"[ARROW ANIM] Moving to exit position {exitTargetPosition}");

            // Step 1: Move entire arrow to exit position
            sequence.Append(transform.DOMove(exitTargetPosition, moveDuration).SetEase(Ease.OutQuad));

            // Step 2: Unparent body parts and hide icon
            sequence.AppendCallback(() =>
            {
                if (iconObject != null) iconObject.SetActive(false); // Hide icon as soon as we leave the grid

                foreach (var part in bodyParts)
                {
                    if (part != null)
                    {
                        part.transform.SetParent(null); // Detach to world
                    }
                }
            });

            // Step 3: Move Head (this object) to Slot immediately
            sequence.Append(transform.DOMove(slotPosition, headMoveToSlotDuration).SetEase(Ease.InOutQuad));

            // Step 4: Head Arrival - Initialize Slot
            sequence.AppendCallback(() =>
            {
                // Trigger callback to set slot color and 0 ammo
                onHeadArrived?.Invoke();

                // Pulse Head
                if (headObject != null)
                {
                    headObject.transform.DOScale(headObject.transform.localScale * 1.3f, 0.1f)
                        .SetLoops(2, LoopType.Yoyo)
                        .SetEase(Ease.OutQuad);
                }
            });

            int totalAmmo = GetAmmoAmount();
            int ammoPerPart = totalAmmo / Length;
            int remainder = totalAmmo % Length;

            // Step 5: Body parts follow to Slot one by one
            if (bodyParts.Count > 0)
            {
                for (int i = 0; i < bodyParts.Count; i++)
                {
                    int bodyIndex = i;
                    GameObject bodyPart = bodyParts[i];

                    if (bodyPart == null) continue;

                    // Create a sub-sequence for this part so we can have intervals
                    sequence.AppendCallback(() =>
                    {
                        if (bodyPart != null)
                        {
                            bodyPart.transform.DOMove(slotPosition, bodyMergeDuration)
                               .SetEase(Ease.InQuad)
                               .OnComplete(() =>
                               {
                                   if (bodyPart != null)
                                   {
                                       // Part arrived at slot
                                       onAmmoIncrement?.Invoke(ammoPerPart);

                                       // Pulse Head again
                                       if (headObject != null)
                                       {
                                           headObject.transform.DOPunchScale(Vector3.one * 0.2f, 0.1f, 1, 0);
                                       }

                                       Destroy(bodyPart); // Remove body part
                                   }
                               });
                        }
                    });

                    sequence.AppendInterval(0.15f); // Delay between parts
                }
            }

            // Step 6: Final Head Processing (head itself counts as ammo + any remainder)
            sequence.AppendCallback(() =>
            {
                int finalHeadAmmo = ammoPerPart + remainder;
                onAmmoIncrement?.Invoke(finalHeadAmmo);
                // Debug.Log($"[ARROW ANIM] Head added {finalHeadAmmo} ammo (base: {ammoPerPart}, remainder: {remainder})");
            });

            // Step 7: Finish
            sequence.AppendInterval(0.2f);
            sequence.OnComplete(() =>
            {
                // Shrink head 
                transform.DOScale(Vector3.zero, 0.2f).OnComplete(() =>
                {
                    Debug.Log("[ARROW ANIM] Complete!");
                    onComplete?.Invoke();
                });
            });

            sequence.Play();
        }


        private void UpdateVisuals()
        {
            // Root stays grid-aligned to make procedural segment offsets simple
            transform.localRotation = Quaternion.identity;
        }
    }
}
