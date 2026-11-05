using System.Collections.Generic;
using UnityEngine;

namespace Edlore.Utilities
{
    [System.Serializable]
    public class ObjectPoolItem
    {
        public PoolObjectsType type;
        public Transform poolParent;
        public int amountToPool;
        public GameObject objectToPool;
        public List<GameObject> pooledObjects;
    }

    public enum PoolObjectsType
    {
        CHARCTER,
        FRIEND,
        RANK,
        REQUESTEDFRIEND,
        RECENTFRIEND,
        ROOMINVITE,
    }

    public class ObjectPooler : MonoBehaviour
    {
        public static ObjectPooler objectInstance;

        [SerializeField]
        private List<ObjectPoolItem> poolItemsList;

        private void Awake()
        {
            if (objectInstance != null && objectInstance != this)
            {
                Destroy(gameObject);
                return;
            }

            objectInstance = this;
            DontDestroyOnLoad(transform.root.gameObject);
        }

        void Start()
        {
            foreach (var item in poolItemsList)
            {
                item.pooledObjects = new List<GameObject>();
                for (int i = 0; i < item.amountToPool; i++)
                {
                    GameObject createdPoolItem = Instantiate(item.objectToPool, item.poolParent);
                    createdPoolItem.SetActive(false);
                    item.pooledObjects.Add(createdPoolItem);
                }
            }
        }

        /// <summary>Returns the first inactive pooled object of the given type, or null when none is free.</summary>
        public GameObject GetPoolItemViaType(PoolObjectsType poolObjectsType)
        {
            foreach (var item in poolItemsList)
            {
                if (item.type != poolObjectsType)
                    continue;

                foreach (GameObject pooledObject in item.pooledObjects)
                {
                    if (pooledObject != null && !pooledObject.activeSelf)
                        return pooledObject;
                }
            }
            return null;
        }

        public void ReturnObjectToPool(PoolObjectsType type, GameObject poolObject)
        {
            Transform parent = poolItemsList.Find(item => item.type == type)?.poolParent;
            poolObject.SetActive(false);
            poolObject.transform.SetParent(parent);
        }
    }
}
