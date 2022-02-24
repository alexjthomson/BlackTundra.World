using BlackTundra.Foundation.Collections.Generic;
using BlackTundra.Foundation.Utility;

using System;
using System.Collections.Generic;

using UnityEngine;

namespace BlackTundra.World.Pooling {

    /// <summary>
    /// Manages a collection of <see cref="IObjectPoolable"/> objects.
    /// </summary>
    public sealed class ObjectPool {

        #region variable

        /// <summary>
        /// Delegate used when a poolable object is required by the <see cref="ObjectPool"/>.
        /// This occurs when there are no free objects in the <see cref="buffer"/> that can be used.
        /// </summary>
        /// <param name="index">Index of the <see cref="IObjectPoolable"/> in the <see cref="ObjectPool"/>.</param>
        /// <returns></returns>
        public delegate IObjectPoolable CreateObjectDelegate(in ObjectPool objectPool, in int index);

        #endregion

        #region variable

        /// <summary>
        /// <see cref="PackedBuffer{T}"/> containing every <see cref="IObjectPoolable"/> that the <see cref="ObjectPool"/> manages.
        /// </summary>
        private readonly PackedBuffer<IObjectPoolable> buffer;

        /// <summary>
        /// <see cref="PackedBuffer{T}"/> containing every active <see cref="IObjectPoolable"/> that the <see cref="ObjectPool"/> manages.
        /// </summary>
        private readonly PackedBuffer<IObjectPoolable> activeBuffer;

        /// <summary>
        /// Callback invoked when there are no free objects in the <see cref="buffer"/> and the <see cref="ObjectPool"/> has room to
        /// expand and create a new object that is free.
        /// </summary>
        private readonly CreateObjectDelegate createObjectCallback;

        /// <summary>
        /// Index of the last object in the <see cref="buffer"/> that was used by the <see cref="ObjectPool"/>.
        /// </summary>
        private int lastIndex;

        #endregion

        #region property

        /// <summary>
        /// Maximum capacity of the <see cref="ObjectPool"/>.
        /// </summary>
        public int Capacity => buffer.Capacity;

        /// <summary>
        /// Total number of objects in the <see cref="ObjectPool"/>.
        /// </summary>
        public int ObjectCount => buffer.Count;

        /// <summary>
        /// Value between <c>0.0</c> (empty) and <c>1.0</c> (full) that describes how full the <see cref="ObjectPool"/> is.
        /// </summary>
        public float Fullness => Mathf.Clamp01(buffer.Count / buffer.Capacity);

        /// <summary>
        /// <c>true</c> if the <see cref="ObjectPool"/> is full.
        /// </summary>
        public bool IsFull => buffer.IsFull;

        /// <param name="index">Index of the <see cref="IObjectPoolable"/> object in the <see cref="ObjectPool"/>.</param>
        /// <returns>
        /// Returns the <see cref="IObjectPoolable"/> object at the specified <paramref name="index"/> in the <see cref="ObjectPool"/>.
        /// </returns>
        /// <seealso cref="Length"/>
        public IObjectPoolable this[in int index] {
            get {
                if (index < 0 || index >= buffer.Count) throw new ArgumentOutOfRangeException(nameof(index));
                return buffer[index];
            }
        }

        /// <summary>
        /// Length of the <see cref="ObjectPool"/> when it is treated like an array.
        /// </summary>
        /// <remarks>
        /// Synonymous with <see cref="ObjectCount"/>.
        /// </remarks>
        /// <seealso cref="this[in int]"/>
        public int Length => buffer.Count;

        /// <summary>
        /// Total number of active <see cref="IObjectPoolable"/> instances in the <see cref="ObjectPool"/>.
        /// </summary>
        /// <seealso cref="GetActiveObj(in int)"/>
        public int ActiveObjCount => activeBuffer.Count;

        #endregion

        #region constructor

        public ObjectPool(in int capacity, in int initialObjectCount, in CreateObjectDelegate createObjectCallback) {
            if (capacity < 1) throw new ArgumentOutOfRangeException($"{nameof(capacity)} must be at least 1.");
            if (initialObjectCount < 0) throw new ArgumentOutOfRangeException(nameof(initialObjectCount));
            if (initialObjectCount > capacity) throw new ArgumentOutOfRangeException($"{nameof(initialObjectCount)} cannot be greater than {nameof(capacity)}.");
            if (createObjectCallback == null) throw new ArgumentNullException(nameof(createObjectCallback));
            this.createObjectCallback = createObjectCallback;
            buffer = new PackedBuffer<IObjectPoolable>(capacity);
            activeBuffer = new PackedBuffer<IObjectPoolable>(0);
            lastIndex = 0;
            if (initialObjectCount > 0) {
                IObjectPoolable poolable;
                for (int i = 0; i < initialObjectCount; i++) {
                    poolable = createObjectCallback(this, i);
                    if (poolable == null) throw new NullReferenceException($"null returned from `{nameof(createObjectCallback)}`.");
                    buffer.AddLast(poolable);
                }
            }
        }

        public ObjectPool(in IEnumerable<IObjectPoolable> objects) {
            buffer = new PackedBuffer<IObjectPoolable>(16);
            activeBuffer = new PackedBuffer<IObjectPoolable>(0);
            foreach (IObjectPoolable obj in objects) {
                if (buffer.IsFull) buffer.Expand(16);
                buffer.AddLast(obj);
            }
            buffer.Shrink();
            if (buffer.Count == 0) throw new ArgumentException($"{nameof(objects)} is empty.");
            createObjectCallback = null;
            lastIndex = 0;
        }

        public ObjectPool(in IEnumerable<IObjectPoolable> objects, in int capacity, in CreateObjectDelegate createObjectCallback) {
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (createObjectCallback == null) throw new ArgumentNullException(nameof(createObjectCallback));
            buffer = new PackedBuffer<IObjectPoolable>(capacity);
            activeBuffer = new PackedBuffer<IObjectPoolable>(0);
            foreach (IObjectPoolable obj in objects) {
                if (buffer.IsFull) throw new ArgumentException($"{nameof(objects)} contains more objects than the {nameof(capacity)} allows.");
                buffer.AddLast(obj);
            }
            this.createObjectCallback = createObjectCallback;
            lastIndex = 0;
        }

        public ObjectPool(in IObjectPoolable[] objects, in int startIndex, in int length, in int capacity, in CreateObjectDelegate createObjectCallback) {
            if (objects == null) throw new ArgumentNullException(nameof(objects));
            int objectCount = objects.Length;
            if (startIndex < 0 || startIndex >= objectCount) throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (length < 0 || length > objectCount - startIndex) throw new ArgumentOutOfRangeException(nameof(length));
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (length > capacity) throw new ArgumentException($"{nameof(length)} cannot be greater than {nameof(capacity)}.");
            if (createObjectCallback == null) throw new ArgumentNullException(nameof(createObjectCallback));
            buffer = new PackedBuffer<IObjectPoolable>(objects, startIndex, length, capacity);
            activeBuffer = new PackedBuffer<IObjectPoolable>(0);
            this.createObjectCallback = createObjectCallback;
            lastIndex = 0;
        }

        #endregion

        #region logic

        #region TryGetNext

        /// <returns>
        /// Returns <c>true</c> if an available <see cref="IObjectPoolable"/> instance was found.
        /// </returns>
        /// <param name="obj">Next available <see cref="IObjectPoolable"/> managed by the <see cref="ObjectPool"/>.</param>
        public bool TryGetNext(out IObjectPoolable obj) {
            int count = buffer.Count;
            // try get available object:
            if (lastIndex > count) { // out of range, iterate entire buffer
                lastIndex = 0;
                for (int i = 0; i < count; i++) {
                    obj = buffer[i];
                    if (obj.IsAvailable(this)) {
                        RegisterActiveObj(obj, i + 1);
                        return true;
                    }
                }
            } else { // in range, iterate buffer in two parts
                for (int i = lastIndex; i < count; i++) {
                    obj = buffer[i];
                    if (obj.IsAvailable(this)) {
                        RegisterActiveObj(obj, i + 1);
                        return true;
                    }
                }
                if (lastIndex > 0) {
                    for (int i = 0; i < lastIndex; i++) {
                        obj = buffer[i];
                        if (obj.IsAvailable(this)) {
                            RegisterActiveObj(obj, i + 1);
                            return true;
                        }
                    }
                }
            }
            // try create available object:
            if (!buffer.IsFull && createObjectCallback != null) {
                obj = createObjectCallback.Invoke(this, count);
                if (obj == null) return false;
                buffer.AddLast(obj);
                RegisterActiveObj(obj, 0);
                return true;
            } else {
                obj = null;
                return false;
            }
        }

        #endregion

        #region RegisterActiveObj

        private void RegisterActiveObj(in IObjectPoolable obj, in int lastIndex) {
            this.lastIndex = lastIndex;
            obj.OnPoolUse(this);
            // ensure active buffer has available space:
            if (activeBuffer.IsFull) {
                int bufferCapacity = buffer.Capacity;
                int expandSize = Mathf.CeilToInt(bufferCapacity * 0.1f);
                int currentSize = activeBuffer.Capacity;
                int newSize = currentSize + expandSize;
                if (newSize > bufferCapacity) {
                    expandSize = bufferCapacity - currentSize; // expand only enough to reach max capacity
                }
                activeBuffer.Expand(expandSize);
            }
            // add the active object to the active buffer:
            activeBuffer.AddLast(obj, true);
        }

        #endregion

        #region ReturnToPool

        /// <summary>
        /// Returns an <paramref name="obj"/> to the <see cref="ObjectPool"/>.
        /// </summary>
        /// <returns>Returns <c>true</c> if the <paramref name="obj"/> was successfully returned to the <see cref="ObjectPool"/>.</returns>
        public bool ReturnToPool(in IObjectPoolable obj) {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (activeBuffer.Remove(obj) > 0) {
                obj.OnRelease(this);
                return true;
            }
            return false;
        }

        #endregion

        #region Dispose

        public void Dispose() {
            activeBuffer.Clear(0);
            int count = buffer.Count;
            IObjectPoolable obj;
            for (int i = count - 1; i >= 0; i--) {
                obj = buffer[i];
                try {
                    obj.Dispose(this);
                } catch (Exception exception) {
                    exception.Handle();
                }
            }
            buffer.Clear();
        }

        #endregion

        #region GetActiveObj

        /// <returns>
        /// Returns the <see cref="IObjectPoolable"/> instance at the specified <paramref name="index"/>.
        /// </returns>
        /// <seealso cref="ActiveObjCount"/>
        public IObjectPoolable GetActiveObj(in int index) {
            if (index < 0 || index >= activeBuffer.Count) throw new ArgumentOutOfRangeException(nameof(index));
            return activeBuffer[index];
        }

        #endregion

        #endregion

    }

}