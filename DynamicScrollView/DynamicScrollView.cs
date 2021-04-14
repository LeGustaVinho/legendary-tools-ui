using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LegendaryTools.UI
{
    [RequireComponent(typeof(ScrollRect))]
    public abstract class DynamicScrollView<TGameObject, TData> : MonoBehaviour
        where TGameObject : Component, DynamicScrollView<TGameObject, TData>.IListingItem
    {
        public interface IListingItem
        {
            void Init(TData item);
        }

        public event Action<TGameObject, TData> OnCreateItem;
        public event Action<TGameObject, TData> OnRemoveItem;

        public List<TGameObject> Listing
        {
            get
            {
                var listView = new List<TGameObject>();
                listView.AddRange(itemsAtSlot.Values);
                return listView;
            }
        }

        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private TGameObject prefab;

        [Header("Slots")] [SerializeField] private int SlotNumInstantiateCallsPerFrame = 10;

        private RectTransform slotPrefab;
        private readonly List<RectTransform> slots = new List<RectTransform>();
        private Coroutine createSlotsRoutine;

        private RectTransform rectTransform;
        private Coroutine generateRoutine;
        private Rect viewportRect;
        private readonly Vector3[] bufferCorners = new Vector3[4];
        private Rect bufferRect;

        private readonly Dictionary<int, TGameObject> itemsAtSlot = new Dictionary<int, TGameObject>();

        public List<TData> DataSource { get; } = new List<TData>();

        public void Generate(TData[] data)
        {
            if (generateRoutine == null) generateRoutine = StartCoroutine(GenerateView(data));
        }

        public void DestroyAllItems()
        {
            foreach (var itemInSlot in itemsAtSlot)
            {
                OnItemRemoved(itemInSlot.Value, DataSource[itemInSlot.Key]);
                OnRemoveItem?.Invoke(itemInSlot.Value, DataSource[itemInSlot.Key]);
                Pool.Destroy(itemInSlot.Value);
            }

            itemsAtSlot.Clear();
        }

        protected void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        protected virtual void Start()
        {
            UpdateViewportRect();

            var newSlotPrefabGo =
                new GameObject("SlotPrefab", typeof(RectTransform), typeof(GameObjectPoolReference));
            slotPrefab = newSlotPrefabGo.GetComponent<RectTransform>();

            scrollRect.onValueChanged.AddListener(OnScrollRectChange);
        }

        protected virtual void OnEnable()
        {
            if (slots.Count != DataSource.Count) generateRoutine = StartCoroutine(GenerateView(DataSource.ToArray()));
        }

        protected virtual void OnDisable()
        {
            if (generateRoutine != null)
            {
                StopCoroutine(generateRoutine);
                generateRoutine = null;
            }
        }

        protected virtual void OnDestroy()
        {
            if (generateRoutine != null)
            {
                StopCoroutine(generateRoutine);
                generateRoutine = null;
            }
#if !UNITY_EDITOR
        foreach (RectTransform slot in slots)
        {
            Pool.Destroy(slot);
        }

        foreach (var pair in itemsAtSlot)
        {
            Pool.Destroy(pair.Value);
        }
#endif
            scrollRect.onValueChanged.RemoveListener(OnScrollRectChange);
        }

        protected virtual void Reset()
        {
            scrollRect = GetComponent<ScrollRect>();
        }

        protected virtual void OnItemCreated(TGameObject item, TData data)
        {
        }

        protected virtual void OnItemRemoved(TGameObject item, TData data)
        {
        }

        private IEnumerator GenerateView(TData[] data)
        {
            DestroyAllItems();

            DataSource.Clear();
            DataSource.AddRange(data);

            if (DataSource.Count > slots.Count)
            {
                var slotsToCreate = Mathf.Clamp(DataSource.Count - slots.Count, 0, SlotNumInstantiateCallsPerFrame);

                while (slotsToCreate != 0)
                {
                    for (var i = 0; i < slotsToCreate; i++)
                    {
                        var newSlot = Pool.Instantiate(slotPrefab);
                        newSlot.SetParent(scrollRect.content);

                        newSlot.localPosition = Vector3.zero;
                        newSlot.localScale = Vector3.one;
                        newSlot.localRotation = Quaternion.identity;

                        slots.Add(newSlot);
                    }

                    yield return new WaitForEndOfFrame(); //wait for layout rebuild
                    yield return
                        new WaitForEndOfFrame(); //wait again to make sure the layout is right on newly created objects

                    UpdateVisibility();

                    slotsToCreate = Mathf.Clamp(DataSource.Count - slots.Count, 0, SlotNumInstantiateCallsPerFrame);
                }
            }
            else if (slots.Count > DataSource.Count)
            {
                var slotsToRemove = slots.Count - DataSource.Count;

                for (var i = 0; i < slotsToRemove; i++)
                {
                    var slotToDestroy = slots[slots.Count - 1];
                    slots.RemoveAt(slots.Count - 1);
                    Pool.Destroy(slotToDestroy);
                }

                yield return new WaitForEndOfFrame();
                UpdateVisibility();
            }
            else //slots.Count == DataSource.Count
            {
                UpdateVisibility();
            }

            generateRoutine = null;
        }

        private void OnScrollRectChange(Vector2 scrollDelta)
        {
            UpdateViewportRect();
            UpdateVisibility();
        }
        
        private void UpdateVisibility()
        {
            for (var i = 0; i < slots.Count; i++)
                if (IsVisible(viewportRect, slots[i]))
                    CreateItemAt(i);
                else
                    DestroyItemAt(i);
        }

        private void CreateItemAt(int index)
        {
            if (slots.Count > index && DataSource.Count > index)
                if (!itemsAtSlot.ContainsKey(index))
                {
                    var newItem = Pool.Instantiate(prefab);
                    var newItemRT = newItem.GetComponent<RectTransform>();
                    var prefabRT = prefab.GetComponent<RectTransform>();

                    newItemRT.SetParent(slots[index]);

                    newItemRT.localPosition = prefabRT.localPosition;
                    newItemRT.localScale = prefabRT.localScale;
                    newItemRT.localRotation = prefabRT.localRotation;

                    itemsAtSlot.Add(index, newItem);
                    newItem.Init(DataSource[index]);

                    OnItemCreated(newItem, DataSource[index]);
                    OnCreateItem?.Invoke(newItem, DataSource[index]);
                }
        }

        private void DestroyItemAt(int index)
        {
            if (itemsAtSlot.TryGetValue(index, out var viewItem))
            {
                itemsAtSlot.Remove(index);
                OnItemRemoved(viewItem, DataSource[index]);
                OnRemoveItem?.Invoke(viewItem, DataSource[index]);

                Pool.Destroy(viewItem);
            }
        }

        private void UpdateViewportRect()
        {
            (scrollRect.viewport != null ? scrollRect.viewport : rectTransform).GetWorldCorners(bufferCorners);

            viewportRect.Set(bufferCorners[1].x, bufferCorners[1].y, Mathf.Abs(bufferCorners[2].x - bufferCorners[1].x),
                Mathf.Abs(bufferCorners[1].y - bufferCorners[0].y));
        }

        private bool IsVisible(Rect parentRect, RectTransform rectTransform)
        {
            if (rectTransform != null)
            {
                rectTransform.GetWorldCorners(bufferCorners);
                var width = Mathf.Abs(bufferCorners[2].x - bufferCorners[1].x);
                var height = Mathf.Abs(bufferCorners[1].y - bufferCorners[0].y);

                bufferRect.Set(bufferCorners[1].x, bufferCorners[1].y + parentRect.height - height, width, height);
                return parentRect.Overlaps(bufferRect);
            }

            return false;
        }
    }
}