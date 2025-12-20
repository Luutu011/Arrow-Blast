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
        [SerializeField] private Color[] colorDefinitions;

        private GameObject headObject;
        private List<GameObject> bodyParts = new List<GameObject>(); // Individual body segments for animation

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
            Debug.Log($"[ARROW INIT] Arrow initialized at ({x}, {y}), Length: {Length}, Body parts will be created");
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
            if (colorDefinitions != null && (int)Color < colorDefinitions.Length)
            {
                return colorDefinitions[(int)Color];
            }

            // Fallback colors if colorDefinitions not set
            switch (Color)
            {
                case BlockColor.Red: return UnityEngine.Color.red;
                case BlockColor.Blue: return UnityEngine.Color.blue;
                case BlockColor.Green: return UnityEngine.Color.green;
                case BlockColor.Yellow: return UnityEngine.Color.yellow;
                case BlockColor.Purple: return new Color(0.5f, 0f, 0.5f);
                case BlockColor.Orange: return new Color(1f, 0.5f, 0f);
                default: return UnityEngine.Color.white;
            }
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
            // 1. Cleanup ALL children to ensure no legacy prefab parts remain
            // We use a while loop because childCount changes as we destroy
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
            bodyParts.Clear();

            if (Segments == null || Segments.Count == 0) return;

            Color arrowColor = GetArrowColor();
            float visualScale = 0.95f; // Slightly smaller than cell to show separation

            // 2. Create Head (at Segments[0])
            headObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            headObject.name = "Head";
            headObject.transform.SetParent(transform);
            headObject.transform.localPosition = Vector3.zero; // Head is always at the arrow's root position
            headObject.transform.localScale = new Vector3(visualScale, visualScale, visualScale) * CellSize;

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

            MeshRenderer headMr = headObject.GetComponent<MeshRenderer>();
            if (headMaterial != null)
            {
                headMr.material = new Material(headMaterial);
                headMr.material.color = arrowColor;
            }
            else
            {
                headMr.material.color = arrowColor;
            }
            Destroy(headObject.GetComponent<Collider>());

            // 3. Create Body Parts (Remaining segments)
            for (int i = 1; i < Segments.Count; i++)
            {
                Vector2Int cell = Segments[i];
                Vector2Int head = Segments[0]; // Reference for offset

                GameObject bodyPart = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bodyPart.name = $"BodyPart_{i}";
                bodyPart.transform.SetParent(transform);

                // Position relative to head in grid space
                // Since the root is NOT rotated, we can use direct grid offsets
                bodyPart.transform.localPosition = new Vector3(
                    (cell.x - head.x) * CellSize,
                    (cell.y - head.y) * CellSize,
                    0
                );

                bodyPart.transform.localScale = new Vector3(visualScale * 0.85f, visualScale * 0.85f, visualScale * 0.85f) * CellSize;
                bodyPart.transform.localRotation = Quaternion.identity;

                MeshRenderer bodyMr = bodyPart.GetComponent<MeshRenderer>();
                if (bodyMaterial != null)
                {
                    bodyMr.material = new Material(bodyMaterial);
                    bodyMr.material.color = arrowColor;
                }
                else
                {
                    bodyMr.material.color = arrowColor;
                }

                Destroy(bodyPart.GetComponent<Collider>());
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

            Debug.Log($"[ARROW ANIM] Moving to exit position {exitTargetPosition}");

            // Step 1: Move entire arrow to exit position
            sequence.Append(transform.DOMove(exitTargetPosition, moveDuration).SetEase(Ease.OutQuad));

            // Step 2: Unparent body parts so they stay at exit while head moves
            sequence.AppendCallback(() =>
            {
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
                                       int ammoForSegment = GetAmmoPerSegment();
                                       onAmmoIncrement?.Invoke(ammoForSegment);

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

            // Step 6: Final Head Processing (head itself counts as ammo)
            sequence.AppendCallback(() =>
            {
                int headAmmo = GetAmmoPerSegment();
                onAmmoIncrement?.Invoke(headAmmo);
                Debug.Log("[ARROW ANIM] Head added its own ammo");
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

        private int GetAmmoPerSegment()
        {
            // Distribute ammo evenly across all segments (head + body)
            int totalAmmo = GetAmmoAmount();
            return Mathf.CeilToInt(totalAmmo / (float)Length);
        }

        private void UpdateVisuals()
        {
            // Root stays grid-aligned to make procedural segment offsets simple
            transform.localRotation = Quaternion.identity;
        }
    }
}
