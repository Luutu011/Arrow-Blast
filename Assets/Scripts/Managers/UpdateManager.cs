using UnityEngine;
using System.Collections.Generic;

namespace ArrowBlast.Managers
{
    public interface IUpdateable
    {
        void ManagedUpdate();
    }

    public interface IFixedUpdateable
    {
        void ManagedFixedUpdate();
    }

    public class UpdateManager : MonoBehaviour
    {
        public static UpdateManager Instance { get; private set; }

        private List<IUpdateable> updateables = new List<IUpdateable>(100);
        private List<IFixedUpdateable> fixedUpdateables = new List<IFixedUpdateable>(50);

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void RegisterUpdateable(IUpdateable updateable)
        {
            if (!updateables.Contains(updateable))
                updateables.Add(updateable);
        }

        public void UnregisterUpdateable(IUpdateable updateable)
        {
            updateables.Remove(updateable);
        }

        public void RegisterFixedUpdateable(IFixedUpdateable fixedUpdateable)
        {
            if (!fixedUpdateables.Contains(fixedUpdateable))
                fixedUpdateables.Add(fixedUpdateable);
        }

        public void UnregisterFixedUpdateable(IFixedUpdateable fixedUpdateable)
        {
            fixedUpdateables.Remove(fixedUpdateable);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = updateables.Count - 1; i >= 0; i--)
            {
                updateables[i].ManagedUpdate();
            }
        }

        private void FixedUpdate()
        {
            for (int i = fixedUpdateables.Count - 1; i >= 0; i--)
            {
                fixedUpdateables[i].ManagedFixedUpdate();
            }
        }
    }
}
