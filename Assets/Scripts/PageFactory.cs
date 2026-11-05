using System;
using System.Collections.Generic;
using Edlore.GamePages;
using UnityEngine;

namespace Edlore.Utilities
{
    public class PageFactory : MonoBehaviour
    {
        [Header("Registered Page Components")]
        [SerializeField]
        private List<BasePage> registeredPages = new();

        private static Dictionary<Type, BasePage> pageFactoryDictionary = new();

        internal static T Get<T>()
            where T : BasePage
        {
            if (pageFactoryDictionary.TryGetValue(typeof(T), out BasePage cached))
                return (T)cached;

            T value = FindObjectOfType<T>();

            if (value != null)
                Add(value);

            return value;
        }

        internal static void Add<T>(T value)
            where T : BasePage
        {
            if (value == null)
                return;

            Type pageType = typeof(T);

            if (pageFactoryDictionary.TryGetValue(pageType, out BasePage existing))
            {
                if (existing != null)
                    return;

                pageFactoryDictionary.Remove(pageType);
            }

            pageFactoryDictionary.Add(pageType, value);

            RefreshInspectorList();
        }

        internal static void Clear()
        {
            pageFactoryDictionary.Clear();
            RefreshInspectorList();
        }

        internal static void Remove<T>(T obj)
            where T : BasePage
        {
            pageFactoryDictionary.Remove(typeof(T));
            RefreshInspectorList();
        }

        public static void Refresh()
        {
            pageFactoryDictionary?.Clear();
            RefreshInspectorList();
        }

        private static void RefreshInspectorList()
        {
            PageFactory factory = FindObjectOfType<PageFactory>();

            if (factory == null)
                return;

            factory.registeredPages.Clear();

            foreach (var item in pageFactoryDictionary)
            {
                if (item.Value != null)
                    factory.registeredPages.Add(item.Value);
            }
        }
    }
}
