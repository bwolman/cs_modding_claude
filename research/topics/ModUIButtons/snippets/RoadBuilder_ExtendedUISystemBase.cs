// Source: RoadBuilder-CSII/RoadBuilder/Systems/UI/ExtendedUISystemBase.cs
// Annotated example of a UISystemBase helper with convenience methods.
//
// Key patterns demonstrated:
// 1. ValueBindingHelper<T> — wraps ValueBinding with dirty-flag batching
// 2. GenericUIWriter<T> — reflection-based serializer for any C# type
// 3. GenericUIReader<T> — reflection-based deserializer for any C# type
// 4. CreateBinding/CreateTrigger helpers — reduce boilerplate
//
// This pattern is common in community mods to avoid implementing IJsonWritable
// on every domain type. Trade-off: runtime reflection cost vs. developer convenience.

using Colossal.UI.Binding;
using Game.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace RoadBuilder.Systems.UI
{
    /// <summary>
    /// Extended UISystemBase with helper methods for creating bindings.
    /// Subclasses call CreateBinding/CreateTrigger in OnCreate().
    /// </summary>
    public abstract partial class ExtendedUISystemBase : UISystemBase
    {
        private readonly List<Action> _updateCallbacks = new();

        protected override void OnUpdate()
        {
            // Run custom update callbacks (e.g., ValueBindingHelper.ForceUpdate)
            foreach (var action in _updateCallbacks)
            {
                action();
            }
            base.OnUpdate();  // Then poll GetterValueBindings
        }

        /// <summary>
        /// Create a read-only ValueBinding (C# -> JS only).
        /// Uses GenericUIWriter for automatic reflection-based serialization.
        /// </summary>
        public ValueBindingHelper<T> CreateBinding<T>(string key, T initialValue)
        {
            var helper = new ValueBindingHelper<T>(
                new ValueBinding<T?>(Mod.Id, key, initialValue, new GenericUIWriter<T?>())
            );
            AddBinding(helper.Binding);
            _updateCallbacks.Add(helper.ForceUpdate);
            return helper;
        }

        /// <summary>
        /// Create a read-write ValueBinding with a TriggerBinding setter.
        /// JS can both read the value and update it via trigger.
        /// </summary>
        public ValueBindingHelper<T> CreateBinding<T>(string key, string setterKey, T initialValue, Action<T>? updateCallBack = null)
        {
            var helper = new ValueBindingHelper<T>(
                new ValueBinding<T?>(Mod.Id, key, initialValue, new GenericUIWriter<T?>()), updateCallBack
            );
            var trigger = new TriggerBinding<T>(Mod.Id, setterKey, helper.UpdateCallback, GenericUIReader<T>.Create());
            AddBinding(helper.Binding);
            AddBinding(trigger);
            _updateCallbacks.Add(helper.ForceUpdate);
            return helper;
        }

        /// <summary>
        /// Create a GetterValueBinding (auto-polled each frame).
        /// </summary>
        public GetterValueBinding<T> CreateBinding<T>(string key, Func<T> getterFunc)
        {
            var binding = new GetterValueBinding<T>(Mod.Id, key, getterFunc, new GenericUIWriter<T>());
            AddUpdateBinding(binding);
            return binding;
        }

        /// <summary>
        /// Create a TriggerBinding (JS -> C#) with 0-4 args.
        /// </summary>
        public TriggerBinding CreateTrigger(string key, Action action)
        {
            var binding = new TriggerBinding(Mod.Id, key, action);
            AddBinding(binding);
            return binding;
        }

        public TriggerBinding<T1> CreateTrigger<T1>(string key, Action<T1> action)
        {
            var binding = new TriggerBinding<T1>(Mod.Id, key, action, GenericUIReader<T1>.Create());
            AddBinding(binding);
            return binding;
        }

        // ... similar for 2, 3, 4 arg versions
    }

    /// <summary>
    /// Wrapper around ValueBinding that batches updates via a dirty flag.
    /// Set .Value to queue an update; ForceUpdate() pushes it during OnUpdate().
    /// This prevents mid-frame partial updates and reduces binding churn.
    /// </summary>
    public class ValueBindingHelper<T>
    {
        private readonly Action<T>? _updateCallBack;
        private T? valueToUpdate;
        private bool dirty;

        public ValueBinding<T?> Binding { get; }

        public T? Value
        {
            get => dirty ? valueToUpdate : Binding.value;
            set
            {
                dirty = true;
                valueToUpdate = value;
            }
        }

        public ValueBindingHelper(ValueBinding<T?> binding, Action<T>? updateCallBack = null)
        {
            Binding = binding;
            _updateCallBack = updateCallBack;
        }

        /// <summary>
        /// Called during OnUpdate(). Pushes queued value to JS if dirty.
        /// </summary>
        public void ForceUpdate()
        {
            if (dirty)
            {
                Binding.Update(valueToUpdate);
                dirty = false;
            }
        }

        /// <summary>
        /// Called by TriggerBinding when JS sets a new value.
        /// </summary>
        public void UpdateCallback(T value)
        {
            Value = value;
            _updateCallBack?.Invoke(value);
        }

        // Implicit conversion for convenient read access
        public static implicit operator T?(ValueBindingHelper<T> helper) => helper.Value;
    }

    /// <summary>
    /// Reflection-based JSON writer. Serializes any C# object by iterating
    /// public properties and fields. Handles primitives, enums, arrays,
    /// IEnumerable, IJsonWritable, and nested objects automatically.
    ///
    /// Trade-off: No need to implement IJsonWritable on each type,
    /// but has runtime reflection overhead.
    /// </summary>
    public class GenericUIWriter<T> : IWriter<T>
    {
        public void Write(IJsonWriter writer, T value)
        {
            WriteGeneric(writer, value);
        }

        private static void WriteGeneric(IJsonWriter writer, object? obj)
        {
            if (obj == null) { writer.WriteNull(); return; }
            if (obj is IJsonWritable w) { w.Write(writer); return; }
            if (obj is int i) { writer.Write(i); return; }
            if (obj is bool b) { writer.Write(b); return; }
            if (obj is string s) { writer.Write(s); return; }
            if (obj is Enum e) { writer.Write(Convert.ToInt32(e)); return; }
            if (obj is Array a) { WriteArray(writer, a); return; }
            if (obj is IEnumerable en) { WriteEnumerable(writer, en); return; }
            // Fallback: reflect over public members
            WriteObject(writer, obj.GetType(), obj);
        }

        private static void WriteObject(IJsonWriter writer, Type type, object obj)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            writer.TypeBegin(type.FullName);
            foreach (var prop in properties)
                { writer.PropertyName(prop.Name); WriteGeneric(writer, prop.GetValue(obj)); }
            foreach (var field in fields)
                { writer.PropertyName(field.Name); WriteGeneric(writer, field.GetValue(obj)); }
            writer.TypeEnd();
        }

        // ... WriteArray and WriteEnumerable omitted for brevity
    }

    /// <summary>
    /// Reflection-based JSON reader. Deserializes JS objects back to C# types
    /// by matching JSON properties to public C# properties/fields.
    /// </summary>
    public class GenericUIReader<T> : IReader<T>
    {
        public static IReader<T> Create()
        {
            // Check built-in readers first, fall back to reflection-based reader
            // Uses reflection to access ValueReaders.s_Readers private dictionary
            var type = typeof(T);
            // ... resolution logic ...
            return new GenericUIReader<T>();
        }

        public void Read(IJsonReader reader, out T value)
        {
            value = (T)ReadGeneric(reader, typeof(T));
        }

        private static object ReadGeneric(IJsonReader reader, Type type)
        {
            // Handles: IJsonReadable, int, bool, string, enum, arrays, List<T>, objects
            // ... type dispatch logic ...
            return ReadObject(reader, type);
        }
    }
}
