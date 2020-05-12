using System;
using System.Collections.Generic;
using System.Diagnostics;
using CSHTML5.Internal;
using CSHTML5.Utility;


#if MIGRATION
namespace System.Windows
#else
namespace Windows.UI.Xaml
#endif
{
    /// <summary>
    ///     Type-specific property metadata
    /// </summary>
    public class PropertyMetadata
    {
        /// <summary>
        ///     Type metadata construction
        /// </summary>
        public PropertyMetadata()
        {
            CallPropertyChangedWhenLoadedIntoVisualTree = WhenToCallPropertyChangedEnum.Never; //CSHTML5
        }

        /// <summary>
        ///     Type metadata construction
        /// </summary>
        /// <param name="defaultValue">Default value of property</param>
        public PropertyMetadata(object defaultValue)
        {
            DefaultValue = defaultValue;
            CallPropertyChangedWhenLoadedIntoVisualTree = WhenToCallPropertyChangedEnum.Never; //CSHTML5
        }

        /// <summary>
        ///     Type meta construction
        /// </summary>
        /// <param name="propertyChangedCallback">Called when the property has been changed</param>
        public PropertyMetadata(PropertyChangedCallback propertyChangedCallback)
        {
            PropertyChangedCallback = propertyChangedCallback;
            CallPropertyChangedWhenLoadedIntoVisualTree = WhenToCallPropertyChangedEnum.Never; //CSHTML5
        }

        /// <summary>
        ///     Type meta construction
        /// </summary>
        /// <param name="defaultValue">Default value of property</param>
        /// <param name="propertyChangedCallback">Called when the property has been changed</param>
        public PropertyMetadata(object defaultValue,
                                PropertyChangedCallback propertyChangedCallback)
        {
            DefaultValue = defaultValue;
            PropertyChangedCallback = propertyChangedCallback;
            CallPropertyChangedWhenLoadedIntoVisualTree = WhenToCallPropertyChangedEnum.Never; //CSHTML5
        }

        /// <summary>
        ///     Type meta construction
        /// </summary>
        /// <param name="defaultValue">Default value of property</param>
        /// <param name="propertyChangedCallback">Called when the property has been changed</param>
        /// <param name="coerceValueCallback">Called on update of value</param>
        public PropertyMetadata(object defaultValue,
                                PropertyChangedCallback propertyChangedCallback,
                                CoerceValueCallback coerceValueCallback)
        {
            DefaultValue = defaultValue;
            PropertyChangedCallback = propertyChangedCallback;
            CoerceValueCallback = coerceValueCallback;
            CallPropertyChangedWhenLoadedIntoVisualTree = WhenToCallPropertyChangedEnum.Never; //CSHTML5
        }

        // 


        /// <summary>
        ///     Default value of property
        /// </summary>
        public object DefaultValue
        {
            get
            {
                DefaultValueFactory defaultFactory = _defaultValue as DefaultValueFactory;
                if (defaultFactory == null)
                {
                    return _defaultValue;
                }
                else
                {
                    return defaultFactory.DefaultValue;
                }
            }

            set
            {
                if (Sealed)
                {
                    throw new InvalidOperationException("Cannot change property metadata after it has been associated with a property.");
                }

                if (value == DependencyProperty.UnsetValue)
                {
                    throw new ArgumentException("Default value cannot be 'Unset'.");
                }

                _defaultValue = value;

                SetModified(MetadataFlags.DefaultValueModifiedID);
            }
        }

        // 


        /// <summary>
        ///     Returns true if the default value is a DefaultValueFactory
        /// </summary>
        internal bool UsingDefaultValueFactory
        {
            get
            {
                return _defaultValue is DefaultValueFactory;
            }
        }

        // 


        /// <summary>
        /// GetDefaultValue returns the default value for a given owner and property.
        /// If the default value is a DefaultValueFactory it will instantiate and cache
        /// the default value on the object.  It must never return an unfrozen default
        /// value if the owner is a frozen Freezable.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        internal object GetDefaultValue(DependencyObject owner, DependencyProperty property)
        {
            Debug.Assert(owner != null && property != null,
                "Caller must provide owner and property or this method will throw in the event of a cache miss.");

            // If we are not using a DefaultValueFactory (common case)
            // just return _defaultValue
            DefaultValueFactory defaultFactory = _defaultValue as DefaultValueFactory;
            if (defaultFactory == null)
            {
                return _defaultValue;
            }

#if WPF_DISABLED

            // If the owner is Sealed it must not have a cached Freezable default value,
            // regardless of whether or not the owner is a Freezable.  The reason
            // for this is that a default created using the FreezableDefaultValueFactory
            // will attempt to set itself as a local value if it is changed.  Since the owner
            // is Sealed this will throw an exception.
            //
            // The solution to this if the owner is a Freezable is to toss out all cached
            // default values when we Seal.  If the owner is not a Freezable we'll promote
            // the value to locally cached.  Either way no Sealed DO can have a cached
            // default value, so we'll return the frozen default value instead.
            if (owner.IsSealed)
            {
                return defaultFactory.DefaultValue;
            }

#endif

            // See if we already have a valid default value that was
            // created by a prior call to GetDefaultValue.
            object result = GetCachedDefaultValue(owner, property);

            if (result != DependencyProperty.UnsetValue)
            {

#if WPF_DISABLED

                // When sealing a DO we toss out all the cached values (see DependencyObject.Seal()).
                // We technically only need to throw out cached values created via the
                // FreezableDefaultValueFactory, but it's more consistent this way.
                Debug.Assert(!owner.IsSealed,
                    "If the owner is Sealed we should not have a cached default value");

#endif

                return result;
            }

            // Otherwise we need to invoke the factory to create the DefaultValue
            // for this property.
            result = defaultFactory.CreateDefaultValue(owner, property);

            // Default value validation ensures that default values do not have
            // thread affinity. This is because a default value is typically 
            // stored in the shared property metadata and handed out to all
            // instances of the owning DependencyObject type.  
            //
            // DefaultValueFactory.CreateDefaultValue ensures that the default  
            // value has thread-affinity to the current thread.  We can thus 
            // skip that portion of the default value validation by calling
            // ValidateFactoryDefaultValue.

#if WPF_DISABLED

            Debug.Assert(!(result is DispatcherObject) || ((DispatcherObject)result).Dispatcher == owner.Dispatcher);

#endif

            property.ValidateFactoryDefaultValue(result);

            // Cache the created DefaultValue so that we can consistently hand
            // out the same default each time we are asked.
            SetCachedDefaultValue(owner, property, result);

            return result;
        }

        // Because the frugalmap is going to be stored in an uncommon field, it would get boxed
        // to avoid this boxing, skip the struct and go straight for the class contained by the
        // struct.  Given the simplicity of this scenario, we can get away with this.
        private object GetCachedDefaultValue(DependencyObject owner, DependencyProperty property)
        {
            FrugalMapBase map = _defaultValueFactoryCache.GetValue(owner);

            if (map == null)
            {
                return DependencyProperty.UnsetValue;
            }

            return map.Search(property.GlobalIndex);
        }

        private void SetCachedDefaultValue(DependencyObject owner, DependencyProperty property, object value)
        {
            FrugalMapBase map = _defaultValueFactoryCache.GetValue(owner);

            if (map == null)
            {
                map = new SingleObjectMap();
                _defaultValueFactoryCache.SetValue(owner, map);
            }
            else if (!(map is HashObjectMap))
            {
                FrugalMapBase newMap = new HashObjectMap();
                map.Promote(newMap);
                map = newMap;
                _defaultValueFactoryCache.SetValue(owner, map);
            }

            map.InsertEntry(property.GlobalIndex, value);
        }

        /// <summary>
        ///     This method causes the DefaultValue cache to be cleared ensuring
        ///     that CreateDefaultValue will be called next time this metadata
        ///     is asked to participate in the DefaultValue factory pattern.
        ///
        ///     This is internal so it can be accessed by subclasses of
        ///     DefaultValueFactory.
        /// </summary>
        internal void ClearCachedDefaultValue(DependencyObject owner, DependencyProperty property)
        {
            FrugalMapBase map = _defaultValueFactoryCache.GetValue(owner);
            if (map.Count == 1)
            {
                _defaultValueFactoryCache.ClearValue(owner);
            }
            else
            {
                map.RemoveEntry(property.GlobalIndex);
            }
        }

#if WPF_DISABLED

        internal static void PromoteAllCachedDefaultValues(DependencyObject owner)
        {
            FrugalMapBase map = _defaultValueFactoryCache.GetValue(owner);

            if (map != null)
            {
                // Iterate through all the items in the map (each representing a DP)
                // and promote them to locally-set.
                map.Iterate(null, _promotionCallback);
            }
        }

        /// <summary>
        /// Removes all cached default values on an object.  It iterates though
        /// each one and, if the cached default is a Freezable, removes its
        /// Changed event handlers. This is called by DependencyObject.Seal()
        /// for Freezable type owners.
        /// </summary>
        /// <param name="owner"></param>
        internal static void RemoveAllCachedDefaultValues(Freezable owner)
        {
            FrugalMapBase map = _defaultValueFactoryCache.GetValue(owner);

            if (map != null)
            {
                // Iterate through all the items in the map (each representing a DP)
                // and remove the promotion handlers
                map.Iterate(null, _removalCallback);

                // Now that they're all clear remove the map.
                _defaultValueFactoryCache.ClearValue(owner);
            }
        }

        /// <summary>
        /// Called once per iteration through the FrugalMap containing all cached default values
        /// for a given DependencyObject. This method removes the promotion handlers on each cached
        /// default and freezes it.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="key">The DP's global index</param>
        /// <param name="value">The cached default</param>
        private static void DefaultValueCacheRemovalCallback(ArrayList list, int key, object value)
        {
            Freezable cachedDefault = value as Freezable;

            if (cachedDefault != null)
            {
                // Freeze fires the Changed event so we need to clear off the handlers before
                // calling it.  Otherwise the promoter would run and attempt to set the
                // cached default as a local value.
                cachedDefault.ClearContextAndHandlers();
                cachedDefault.Freeze();
            }
        }

        /// <summary>
        /// Called once per iteration through the FrugalMap containing all cached default values
        /// for a given DependencyObject. This method promotes each of the defaults to locally-set.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="key">The DP's global index</param>
        /// <param name="value">The cached default</param>
        private static void DefaultValueCachePromotionCallback(ArrayList list, int key, object value)
        {
            Freezable cachedDefault = value as Freezable;

            if (cachedDefault != null)
            {
                // The only way to promote a cached default is to fire its Changed event.
                cachedDefault.FireChanged();
            }
        }

#endif

        /// <summary>
        ///     Whether the DefaultValue was explictly set - needed to know if the
        /// value should be used in Register.
        /// </summary>
        internal bool DefaultValueWasSet()
        {
            return IsModified(MetadataFlags.DefaultValueModifiedID);
        }

        /// <summary>
        ///     Property changed callback
        /// </summary>
        public PropertyChangedCallback PropertyChangedCallback
        {
            get { return _propertyChangedCallback; }
            set
            {
                if (Sealed)
                {
                    throw new InvalidOperationException("Cannot change property metadata after it has been associated with a property.");
                }

                _propertyChangedCallback = value;
            }
        }

        /// <summary>
        ///     Specialized callback invoked upon a call to UpdateValue
        /// </summary>
        /// <remarks>
        ///     Used for "coercing" effective property value without actually subclassing
        /// </remarks>
        public CoerceValueCallback CoerceValueCallback
        {
            get { return _coerceValueCallback; }
            set
            {
                if (Sealed)
                {
                    throw new InvalidOperationException("Cannot change property metadata after it has been associated with a property.");
                }

                _coerceValueCallback = value;
            }
        }

#if WPF_DISABLED

        /// <summary>
        ///     Specialized callback for remote storage of value for read-only properties
        /// </summary>
        /// <remarks>
        ///     This is used exclusively by FrameworkElement.ActualWidth and ActualHeight to save 48 bytes
        ///     of state per FrameworkElement.
        /// </remarks>
        internal virtual GetReadOnlyValueCallback GetReadOnlyValueCallback
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        ///     Specialized callback invoked for each property when a Freezable
        ///     object is frozen.
        /// </summary>
        /// <remarks>
        ///     Used to provide specialized behavior when freezing an object
        ///     that a property has been set on.  This callback can be used to
        ///     decide whether to do a "deep" freeze, a "shallow" freeze, to
        ///     fail the freeze attempt, etc.
        /// </remarks>
        [FriendAccessAllowed] // Currently used by Storyboard in PresentationFramework.
        internal FreezeValueCallback FreezeValueCallback
        {
            get
            {
                if(_freezeValueCallback != null)
                {
                    return _freezeValueCallback;
                }
                else
                {
                    return _defaultFreezeValueCallback;
                }
            }

            set
            {
                if (Sealed)
                {
                    throw new InvalidOperationException(SR.Get(SRID.TypeMetadataCannotChangeAfterUse));
                }

                _freezeValueCallback = value;
            }
        }

        private static bool DefaultFreezeValueCallback(
            DependencyObject d,
            DependencyProperty dp,
            EntryIndex entryIndex,
            PropertyMetadata metadata,
            bool isChecking)
        {
            // The expression check only needs to be done when isChecking is true
            // because if we return false here the Freeze() call will fail.
            if (isChecking)
            {
                if (d.HasExpression(entryIndex, dp))
                {
                    if (TraceFreezable.IsEnabled)
                    {
                        TraceFreezable.Trace(
                            TraceEventType.Warning,
                            TraceFreezable.UnableToFreezeExpression,
                            d,
                            dp,
                            dp.OwnerType);
                    }

                    return false;
                }
            }

            if (!dp.IsValueType)
            {
                object value =
                    d.GetValueEntry(
                        entryIndex,
                        dp,
                        metadata,
                        RequestFlags.FullyResolved).Value;

                if (value != null)
                {
                    Freezable valueAsFreezable = value as Freezable;

                    if (valueAsFreezable != null)
                    {
                        if (!valueAsFreezable.Freeze(isChecking))
                        {
                            if (TraceFreezable.IsEnabled)
                            {
                                TraceFreezable.Trace(
                                    TraceEventType.Warning,
                                    TraceFreezable.UnableToFreezeFreezableSubProperty,
                                    d,
                                    dp,
                                    dp.OwnerType);
                            }

                            return false;
                        }
                    }
                    else  // not a Freezable
                    {
                        DispatcherObject valueAsDispatcherObject = value as DispatcherObject;

                        if (valueAsDispatcherObject != null)
                        {
                            if (valueAsDispatcherObject.Dispatcher == null)
                            {
                                // The property is a free-threaded DispatcherObject; since it's
                                // already free-threaded it doesn't prevent this Freezable from
                                // becoming free-threaded too.
                                // It is up to the creator of this type to ensure that the
                                // DispatcherObject is actually immutable
                            }
                            else
                            {
                                // The value of this property derives from DispatcherObject and
                                // has thread affinity; return false.

                                if (TraceFreezable.IsEnabled)
                                {
                                    TraceFreezable.Trace(
                                        TraceEventType.Warning,
                                        TraceFreezable.UnableToFreezeDispatcherObjectWithThreadAffinity,
                                        d,
                                        dp,
                                        dp.OwnerType,
                                        valueAsDispatcherObject );
                                }

                                return false;
                            }
                        }

                        // The property isn't a DispatcherObject.  It may be immutable (such as a string)
                        // or the user may have made it thread-safe.  It's up to the creator of the type to
                        // do the right thing; we return true as an extensibility point.
                    }
                }
            }

            return true;
        }

        private static FreezeValueCallback _defaultFreezeValueCallback = DefaultFreezeValueCallback;

#endif

        /// <summary>
        ///     Creates a new instance of this property metadata.  This method is used
        ///     when metadata needs to be cloned.  After CreateInstance is called the
        ///     framework will call Merge to merge metadata into the new instance.
        ///     Deriving classes must override this and return a new instance of
        ///     themselves.
        /// </summary>
        internal virtual PropertyMetadata CreateInstance()
        {
            return new PropertyMetadata();
        }

        //
        // Returns a copy of this property metadata by calling CreateInstance
        // and then Merge
        //
        internal PropertyMetadata Copy(DependencyProperty dp)
        {
            PropertyMetadata newMetadata = CreateInstance();
            newMetadata.InvokeMerge(this, dp);
            return newMetadata;
        }

        /// <summary>
        ///     Merge set source state into this
        /// </summary>
        /// <remarks>
        ///     Used when overriding metadata
        /// </remarks>
        /// <param name="baseMetadata">Base metadata to merge</param>
        /// <param name="dp">DependencyProperty that this metadata is being applied to</param>
        protected virtual void Merge(PropertyMetadata baseMetadata, DependencyProperty dp)
        {
            if (baseMetadata == null)
            {
                throw new ArgumentNullException("baseMetadata");
            }

            if (Sealed)
            {
                throw new InvalidOperationException("Cannot change property metadata after it has been associated with a property.");
            }

            // Merge source metadata into this

            // Take source default if this default was never set
            if (!IsModified(MetadataFlags.DefaultValueModifiedID))
            {
                _defaultValue = baseMetadata.DefaultValue;
            }

            if (baseMetadata.PropertyChangedCallback != null)
            {
                // All delegates are MulticastDelegate.  Non-multicast "Delegate"
                //  was designed and is documented in MSDN.  But for all practical
                //  purposes, it was actually cut before v1.0 of the CLR shipped.

                // Build the handler list such that handlers added
                // via OverrideMetadata are called last (base invocation first)
                Delegate[] handlers = baseMetadata.PropertyChangedCallback.GetInvocationList();
                if (handlers.Length > 0)
                {
                    PropertyChangedCallback headHandler = (PropertyChangedCallback)handlers[0];
                    for (int i = 1; i < handlers.Length; i++)
                    {
                        headHandler += (PropertyChangedCallback)handlers[i];
                    }
                    headHandler += _propertyChangedCallback;
                    _propertyChangedCallback = headHandler;
                }
            }

            #region CSHTML5 Internal
            
            if (GetCSSEquivalent == null)
            {
                GetCSSEquivalent = baseMetadata.GetCSSEquivalent;
            }

            if (GetCSSEquivalents == null)
            {
                GetCSSEquivalents = baseMetadata.GetCSSEquivalents;
            }

            if (_methodToUpdateDom == null)
            {
                _methodToUpdateDom = baseMetadata.MethodToUpdateDom;
            }

            #endregion CSHTML5 Internal

            if (_coerceValueCallback == null)
            {
                _coerceValueCallback = baseMetadata.CoerceValueCallback;
            }

#if WPF_DISABLED

            // FreezeValueCallback not combined
            if (_freezeValueCallback == null)
            {
                _freezeValueCallback = baseMetadata.FreezeValueCallback;
            }

#endif
        }

        internal void InvokeMerge(PropertyMetadata baseMetadata, DependencyProperty dp)
        {
            Merge(baseMetadata, dp);
        }


        /// <summary>
        ///     Notification that this metadata has been applied to a property
        ///     and the metadata is being sealed
        /// </summary>
        /// <remarks>
        ///     Normally, any mutability of the data structure should be marked
        ///     as immutable at this point
        /// </remarks>
        /// <param name="dp">DependencyProperty</param>
        /// <param name="targetType">Type associating metadata (null if default metadata)</param>
        protected virtual void OnApply(DependencyProperty dp, Type targetType)
        {
        }


        /// <summary>
        ///     Determines if the metadata has been applied to a property resulting
        ///     in the sealing (immutability) of the instance
        /// </summary>
        protected bool IsSealed
        {
            get { return Sealed; }
        }


        internal void Seal(DependencyProperty dp, Type targetType)
        {
            // CALLBACK
            OnApply(dp, targetType);

            Sealed = true;
        }

        internal bool IsDefaultValueModified { get { return IsModified(MetadataFlags.DefaultValueModifiedID); } }

        internal bool IsInherited
        {
            get { return (MetadataFlags.Inherited & _flags) != 0; ; }
            set
            {
                if (value)
                {
                    _flags |= MetadataFlags.Inherited;
                }
                else
                {
                    _flags &= (~MetadataFlags.Inherited);
                }
            }
        }

        private object _defaultValue;
        private PropertyChangedCallback _propertyChangedCallback;
        private CoerceValueCallback _coerceValueCallback;

#if WPF_DISABLED

        private FreezeValueCallback _freezeValueCallback;

#endif

        // Enhancement idea: DefaultValueFactory bit
        //    If a bit opens up in MetadataFlags we should
        //    track the factory with a bit rather than casting
        //    every time.
        internal enum MetadataFlags : uint
        {
            DefaultValueModifiedID = 0x00000001,
            SealedID = 0x00000002,
            // Unused                                    = 0x00000004,
            // Unused                                    = 0x00000008,
            Inherited = 0x00000010,

#if WPF_DISABLED
            UI_IsAnimationProhibitedID = 0x00000020, // True if peer refers to an owner's animation peer property; False if Peer refers to the animation peer's owner property

            FW_AffectsMeasureID = 0x00000040,
            FW_AffectsArrangeID = 0x00000080,
            FW_AffectsParentMeasureID = 0x00000100,
            FW_AffectsParentArrangeID = 0x00000200,
            FW_AffectsRenderID = 0x00000400,
            FW_OverridesInheritanceBehaviorID = 0x00000800,
            FW_IsNotDataBindableID = 0x00001000,
            FW_BindsTwoWayByDefaultID = 0x00002000,
            FW_ShouldBeJournaledID = 0x00004000,
            FW_SubPropertiesDoNotAffectRenderID = 0x00008000,
            FW_SubPropertiesDoNotAffectRenderModifiedID = 0x00010000,
            // Unused                                    = 0x00020000,
            // Unused                                    = 0x00040000,
            // Unused                                    = 0x00080000,
            FW_InheritsModifiedID = 0x00100000,
            FW_OverridesInheritanceBehaviorModifiedID = 0x00200000,
            // Unused                                    = 0x00400000,
            // Unused                                    = 0x00800000,
            FW_ShouldBeJournaledModifiedID = 0x01000000,
            FW_UpdatesSourceOnLostFocusByDefaultID = 0x02000000,
            FW_DefaultUpdateSourceTriggerModifiedID = 0x04000000,
            FW_ReadOnlyID = 0x08000000,
            // Unused                                    = 0x10000000,
            // Unused                                    = 0x20000000,
            FW_DefaultUpdateSourceTriggerEnumBit1 = 0x40000000, // Must match constants used in FrameworkPropertyMetadata
            FW_DefaultUpdateSourceTriggerEnumBit2 = 0x80000000, // Must match constants used in FrameworkPropertyMetadata
#endif
        }


        // PropertyMetadata, UIPropertyMetadata, and FrameworkPropertyMetadata.
        internal MetadataFlags _flags;

        private void SetModified(MetadataFlags id) { _flags |= id; }
        private bool IsModified(MetadataFlags id) { return (id & _flags) != 0; }

        /// <summary>
        ///     Write a flag value
        /// </summary>
        internal void WriteFlag(MetadataFlags id, bool value)
        {
            if (value)
            {
                _flags |= id;
            }
            else
            {
                _flags &= (~id);
            }
        }

        /// <summary>
        ///     Read a flag value
        /// </summary>
        internal bool ReadFlag(MetadataFlags id) { return (id & _flags) != 0; }

        internal bool Sealed
        {
            get { return ReadFlag(MetadataFlags.SealedID); }
            set { WriteFlag(MetadataFlags.SealedID, value); }
        }

        // We use this uncommon field to stash values created by our default value factory
        // in the owner's _localStore.
#if WPF_DISABLED
        private static readonly UncommonField<FrugalMapBase> _defaultValueFactoryCache = new UncommonField<FrugalMapBase>();
#else
        private static readonly INTERNAL_DefaultValueStore<FrugalMapBase> _defaultValueFactoryCache = new INTERNAL_DefaultValueStore<FrugalMapBase>();
#endif
#if WPF_DISABLED
        private static FrugalMapIterationCallback _removalCallback = new FrugalMapIterationCallback(DefaultValueCacheRemovalCallback);
        private static FrugalMapIterationCallback _promotionCallback = new FrugalMapIterationCallback(DefaultValueCachePromotionCallback);
#endif


        #region CSHTML5 Internal

        private WhenToCallPropertyChangedEnum _callPropertyChangedWhenLoadedIntoVisualTree;
        /// <summary>
        /// Determines if the callback method should be called when the element is added to the visual tree.
        /// </summary>
        public WhenToCallPropertyChangedEnum CallPropertyChangedWhenLoadedIntoVisualTree
        {
            get
            {
                return _callPropertyChangedWhenLoadedIntoVisualTree;
            }
            set
            {
                if (Sealed)
                {
                    throw new InvalidOperationException("Cannot change property metadata after it has been associated with a property.");
                }

                _callPropertyChangedWhenLoadedIntoVisualTree = value;
            }
        }

        public bool Inherits
        {
            get
            {
                return IsInherited;
            }
            set
            {
                if (Sealed)
                {
                    throw new InvalidOperationException("Cannot change property metadata after it has been associated with a property.");
                }

                IsInherited = value;
            }
        }

        private MethodToUpdateDom _methodToUpdateDom;
        public MethodToUpdateDom MethodToUpdateDom
        {
            get
            {
                return this._methodToUpdateDom;
            }
            set
            {
                if (Sealed)
                {
                    throw new InvalidOperationException("Cannot change property metadata after it has been associated with a property.");
                }

                _methodToUpdateDom = value;
            }
        }

        internal CSSEquivalentGetter GetCSSEquivalent;
        internal CSSEquivalentsGetter GetCSSEquivalents;

        #endregion
    }

#if WPF_DISABLED

    /// <summary>
    ///     GetReadOnlyValueCallback -- a very specialized callback that allows storage for read-only properties
    ///     to be managed outside of the effective value store on a DO.  This optimization is restricted to read-only
    ///     properties because read-only properties can only have a value explicitly set by the keeper of the key, so
    ///     it eliminates the possibility of a self-managed store missing modifiers such as expressions, coercion,
    ///     and animation.
    /// </summary>
    internal delegate object GetReadOnlyValueCallback(DependencyObject d, out BaseValueSourceInternal source);

#endif

    #region CSHTML5 Internal

    internal delegate CSSEquivalent CSSEquivalentGetter(DependencyObject d);
    internal delegate List<CSSEquivalent> CSSEquivalentsGetter(DependencyObject d);
    public delegate void MethodToUpdateDom(DependencyObject d, object newValue);

    internal class INTERNAL_DefaultValueStore<T> // This replaces UncommonField and is used to store defaultValues generated by a DefaultValueFactory.
    {
        private readonly Dictionary<DependencyObject, T> _store;
        private readonly T _defaultValue;
        private bool _hasBeenSet;

        public INTERNAL_DefaultValueStore() : this(default(T))
        {

        }

        public INTERNAL_DefaultValueStore(T defaultValue)
        {
            _defaultValue = defaultValue;
            _store = new Dictionary<DependencyObject, T>();
        }

        public T GetValue(DependencyObject instance)
        {
            if (instance != null)
            {
                if (_hasBeenSet)
                {
                    T value;
                    if (_store.TryGetValue(instance, out value))
                    {
                        return value;
                    }
                    return _defaultValue;
                }
                else
                {
                    return _defaultValue;
                }
            }
            else
            {
                throw new ArgumentNullException("instance");
            }
        }

        public void SetValue(DependencyObject instance, T value)
        {
            if (instance != null)
            {
                if (!object.ReferenceEquals(value, _defaultValue))
                {
                    _store[instance] = value;
                    _hasBeenSet = true;
                }
                else
                {
                    _store.Remove(instance);
                }
            }
            else
            {
                throw new ArgumentNullException("instance");
            }
        }

        public void ClearValue(DependencyObject instance)
        {
            if (instance != null)
            {
                _store.Remove(instance);
            }
            else
            {
                throw new ArgumentNullException("instance");
            }
        }

    }

    #endregion
}
