using System;
using System.Collections;
using CSHTML5.Internal;
using CSHTML5.Utility;

#if MIGRATION
using System.Windows.Data;
#else
using Windows.UI.Xaml.Data;
#endif

#if MIGRATION
namespace System.Windows
#else
namespace Windows.UI.Xaml
#endif
{
    public sealed partial class DependencyProperty
    {
        /// <summary>
        ///     Register a Dependency Property
        /// </summary>
        /// <param name="name">Name of property</param>
        /// <param name="propertyType">Type of the property</param>
        /// <param name="ownerType">Type that is registering the property</param>
        /// <returns>Dependency Property</returns>
        public static DependencyProperty Register(string name, Type propertyType, Type ownerType)
        {
            // Forwarding
            return Register(name, propertyType, ownerType, null, null);
        }

        /// <summary>
        ///     Register a Dependency Property
        /// </summary>
        /// <param name="name">Name of property</param>
        /// <param name="propertyType">Type of the property</param>
        /// <param name="ownerType">Type that is registering the property</param>
        /// <param name="typeMetadata">Metadata to use if current type doesn't specify type-specific metadata</param>
        /// <returns>Dependency Property</returns>
        public static DependencyProperty Register(string name, Type propertyType, Type ownerType, PropertyMetadata typeMetadata)
        {
            // Forwarding
            return Register(name, propertyType, ownerType, typeMetadata, null);
        }

        /// <summary>
        ///     Register a Dependency Property
        /// </summary>
        /// <param name="name">Name of property</param>
        /// <param name="propertyType">Type of the property</param>
        /// <param name="ownerType">Type that is registering the property</param>
        /// <param name="typeMetadata">Metadata to use if current type doesn't specify type-specific metadata</param>
        /// <param name="validateValueCallback">Provides additional value validation outside automatic type validation</param>
        /// <returns>Dependency Property</returns>
        public static DependencyProperty Register(string name, Type propertyType, Type ownerType, PropertyMetadata typeMetadata, ValidateValueCallback validateValueCallback)
        {
            RegisterParameterValidation(name, propertyType, ownerType);

            // Register an attached property
            PropertyMetadata defaultMetadata = null;
            if (typeMetadata != null && typeMetadata.DefaultValueWasSet())
            {
                defaultMetadata = new PropertyMetadata(typeMetadata.DefaultValue);
            }

            DependencyProperty property = RegisterCommon(name, propertyType, ownerType, defaultMetadata, validateValueCallback);

            if (typeMetadata != null)
            {
                // Apply type-specific metadata to owner type only
                property.OverrideMetadata(ownerType, typeMetadata);
            }

            return property;
        }

#if WPF_DISABLED

        /// <summary>
        ///  Simple registration, metadata, validation, and a read-only property
        /// key.  Calling this version restricts the property such that it can
        /// only be set via the corresponding overload of DependencyObject.SetValue.
        /// </summary>
        public static DependencyPropertyKey RegisterReadOnly(
            string name,
            Type propertyType,
            Type ownerType,
            PropertyMetadata typeMetadata )
        {
            return RegisterReadOnly( name, propertyType, ownerType, typeMetadata, null );
        }

        /// <summary>
        ///  Simple registration, metadata, validation, and a read-only property
        /// key.  Calling this version restricts the property such that it can
        /// only be set via the corresponding overload of DependencyObject.SetValue.
        /// </summary>
        public static DependencyPropertyKey RegisterReadOnly(
            string name,
            Type propertyType,
            Type ownerType,
            PropertyMetadata typeMetadata,
            ValidateValueCallback validateValueCallback )
        {
            RegisterParameterValidation(name, propertyType, ownerType);

            PropertyMetadata defaultMetadata = null;

            if (typeMetadata != null && typeMetadata.DefaultValueWasSet())
            {
                defaultMetadata = new PropertyMetadata(typeMetadata.DefaultValue);
            }
            else
            {
                defaultMetadata = AutoGeneratePropertyMetadata(propertyType,validateValueCallback,name,ownerType);
            }

            //  We create a DependencyPropertyKey at this point with a null property
            // and set that in the _readOnlyKey field.  This is so the property is
            // marked as requiring a key immediately.  If something fails in the
            // initialization path, the property is still marked as needing a key.
            //  This is better than the alternative of creating and setting the key
            // later, because if that code fails the read-only property would not
            // be marked read-only.  The intent of this mildly convoluted code
            // is so we fail securely.
            DependencyPropertyKey authorizationKey = new DependencyPropertyKey(null); // No property yet, use null as placeholder.

            DependencyProperty property = RegisterCommon(name, propertyType, ownerType, defaultMetadata, validateValueCallback);

            property._readOnlyKey = authorizationKey;

            authorizationKey.SetDependencyProperty(property);

            if (typeMetadata == null )
            {
                // No metadata specified, generate one so we can specify the authorized key.
                typeMetadata = AutoGeneratePropertyMetadata(propertyType,validateValueCallback,name,ownerType);
            }

            // Authorize registering type for read-only access, create key.
#pragma warning suppress 6506 // typeMetadata is never null, since we generate default metadata if none is provided.

            // Apply type-specific metadata to owner type only
            property.OverrideMetadata(ownerType, typeMetadata, authorizationKey);

            return authorizationKey;
        }

        /// <summary>
        ///  Simple registration, metadata, validation, and a read-only property
        /// key.  Calling this version restricts the property such that it can
        /// only be set via the corresponding overload of DependencyObject.SetValue.
        /// </summary>
        public static DependencyPropertyKey RegisterAttachedReadOnly(string name, Type propertyType, Type ownerType, PropertyMetadata defaultMetadata)
        {
            return RegisterAttachedReadOnly( name, propertyType, ownerType, defaultMetadata, null );
        }

        /// <summary>
        ///  Simple registration, metadata, validation, and a read-only property
        /// key.  Calling this version restricts the property such that it can
        /// only be set via the corresponding overload of DependencyObject.SetValue.
        /// </summary>
        public static DependencyPropertyKey RegisterAttachedReadOnly(string name, Type propertyType, Type ownerType, PropertyMetadata defaultMetadata, ValidateValueCallback validateValueCallback)
        {
            RegisterParameterValidation(name, propertyType, ownerType);

            // Establish default metadata for all types, if none is provided
            if (defaultMetadata == null)
            {
                defaultMetadata = AutoGeneratePropertyMetadata( propertyType, validateValueCallback, name, ownerType );
            }

            //  We create a DependencyPropertyKey at this point with a null property
            // and set that in the _readOnlyKey field.  This is so the property is
            // marked as requiring a key immediately.  If something fails in the
            // initialization path, the property is still marked as needing a key.
            //  This is better than the alternative of creating and setting the key
            // later, because if that code fails the read-only property would not
            // be marked read-only.  The intent of this mildly convoluted code
            // is so we fail securely.
            DependencyPropertyKey authorizedKey = new DependencyPropertyKey(null);

            DependencyProperty property = RegisterCommon( name, propertyType, ownerType, defaultMetadata, validateValueCallback);

            property._readOnlyKey = authorizedKey;

            authorizedKey.SetDependencyProperty(property);

            return authorizedKey;
        }

#endif

        /// <summary>
        ///     Register an attached Dependency Property
        /// </summary>
        /// <param name="name">Name of property</param>
        /// <param name="propertyType">Type of the property</param>
        /// <param name="ownerType">Type that is registering the property</param>
        /// <returns>Dependency Property</returns>
        public static DependencyProperty RegisterAttached(string name, Type propertyType, Type ownerType)
        {
            // Forwarding
            return RegisterAttached(name, propertyType, ownerType, null, null);
        }

        /// <summary>
        ///     Register an attached Dependency Property
        /// </summary>
        /// <param name="name">Name of property</param>
        /// <param name="propertyType">Type of the property</param>
        /// <param name="ownerType">Type that is registering the property</param>
        /// <param name="defaultMetadata">Metadata to use if current type doesn't specify type-specific metadata</param>
        /// <returns>Dependency Property</returns>
        public static DependencyProperty RegisterAttached(string name, Type propertyType, Type ownerType, PropertyMetadata defaultMetadata)
        {
            // Forwarding
            return RegisterAttached(name, propertyType, ownerType, defaultMetadata, null);
        }

        /// <summary>
        ///     Register an attached Dependency Property
        /// </summary>
        /// <param name="name">Name of property</param>
        /// <param name="propertyType">Type of the property</param>
        /// <param name="ownerType">Type that is registering the property</param>
        /// <param name="defaultMetadata">Metadata to use if current type doesn't specify type-specific metadata</param>
        /// <param name="validateValueCallback">Provides additional value validation outside automatic type validation</param>
        /// <returns>Dependency Property</returns>
        public static DependencyProperty RegisterAttached(string name, Type propertyType, Type ownerType, PropertyMetadata defaultMetadata, ValidateValueCallback validateValueCallback)
        {
            RegisterParameterValidation(name, propertyType, ownerType);

            DependencyProperty dp = RegisterCommon(name, propertyType, ownerType, defaultMetadata, validateValueCallback);

            // CSHTML5
            dp.IsAttached = true;

            return dp;
        }

        private static void RegisterParameterValidation(string name, Type propertyType, Type ownerType)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw new ArgumentException("Parameter cannot be a zero-length string.");
            }

            if (ownerType == null)
            {
                throw new ArgumentNullException("ownerType");
            }

            if (propertyType == null)
            {
                throw new ArgumentNullException("propertyType");
            }
        }

        private static DependencyProperty RegisterCommon(string name, Type propertyType, Type ownerType, PropertyMetadata defaultMetadata, ValidateValueCallback validateValueCallback)
        {
            FromNameKey key = new FromNameKey(name, ownerType);
            lock (Synchronized)
            {
                if (PropertyFromName.Contains(key))
                {
                    throw new ArgumentException(string.Format("'{0}' property was already registered by '{1}'.", name, ownerType.Name));
                }
            }

            // Establish default metadata for all types, if none is provided
            if (defaultMetadata == null)
            {
                defaultMetadata = AutoGeneratePropertyMetadata(propertyType, validateValueCallback, name, ownerType);
            }
            else // Metadata object is provided.
            {
                // If the defaultValue wasn't specified auto generate one
                if (!defaultMetadata.DefaultValueWasSet())
                {
                    defaultMetadata.DefaultValue = AutoGenerateDefaultValue(propertyType);
                }

                ValidateMetadataDefaultValue(defaultMetadata, propertyType, name, validateValueCallback);
            }

            // Create property
            DependencyProperty dp = new DependencyProperty(name, propertyType, ownerType, defaultMetadata, validateValueCallback);

            // Seal (null means being used for default metadata, calls OnApply)
            defaultMetadata.Seal(dp, null);

            if (defaultMetadata.IsInherited)
            {
                dp._packedData |= Flags.IsPotentiallyInherited;
            }

            if (defaultMetadata.UsingDefaultValueFactory)
            {
                dp._packedData |= Flags.IsPotentiallyUsingDefaultValueFactory;
            }

            // Map owner type to this property
            // Build key
            lock (Synchronized)
            {
                PropertyFromName[key] = dp;
            }

#if WPF_DISABLED

            if( TraceDependencyProperty.IsEnabled )
            {
                TraceDependencyProperty.TraceActivityItem(
                    TraceDependencyProperty.Register,
                    dp,
                    dp.OwnerType );
            }

#endif

            return dp;
        }

        private static object AutoGenerateDefaultValue(
            Type propertyType)
        {
            // Default per-type metadata not provided, create
            object defaultValue = null;

            // Auto-assigned default value
            if (propertyType.IsValueType)
            {
                // Value-types have default-constructed type default values
                defaultValue = Activator.CreateInstance(propertyType);
            }

            return defaultValue;
        }

        private static PropertyMetadata AutoGeneratePropertyMetadata(
            Type propertyType,
            ValidateValueCallback validateValueCallback,
            string name,
            Type ownerType)
        {
            // Default per-type metadata not provided, create
            object defaultValue = AutoGenerateDefaultValue(propertyType);

            // If a validator is passed in, see if the default value makes sense.
            if (validateValueCallback != null &&
                !validateValueCallback(defaultValue))
            {
                // Didn't work - require the caller to specify one.
                throw new ArgumentException(string.Format("Cannot automatically generate a valid default value for property '{0}'. Specify a default value explicitly when owner type '{1}' is registering this DependencyProperty.", name, ownerType.Name));
            }

            return new PropertyMetadata(defaultValue);
        }

        // Validate the default value in the given metadata
        private static void ValidateMetadataDefaultValue(
            PropertyMetadata defaultMetadata,
            Type propertyType,
            string propertyName,
            ValidateValueCallback validateValueCallback)
        {
            // If we are registered to use the DefaultValue factory we can
            // not validate the DefaultValue at registration time, so we
            // early exit.
            if (defaultMetadata.UsingDefaultValueFactory)
            {
                return;
            }

#if WPF_DISABLED
             ValidateDefaultValueCommon(defaultMetadata.DefaultValue, propertyType,
                propertyName, validateValueCallback, /*checkThreadAffinity = */ true);
#else
            ValidateDefaultValueCommon(defaultMetadata.DefaultValue, propertyType, propertyName, validateValueCallback);
#endif
        }

        // Validate the given default value, used by PropertyMetadata.GetDefaultValue()
        // when the DefaultValue factory is used.
        // These default values are allowed to have thread-affinity.
        internal void ValidateFactoryDefaultValue(object defaultValue)
        {
            ValidateDefaultValueCommon(defaultValue, PropertyType, Name, ValidateValueCallback);
        }

        private static void ValidateDefaultValueCommon(
            object defaultValue,
            Type propertyType,
            string propertyName,
            ValidateValueCallback validateValueCallback
#if WPF_DISABLED
            , bool checkThreadAffinity
#endif
            )
        {
            // Ensure default value is the correct type
            if (!IsValidType(defaultValue, propertyType))
            {
                throw new ArgumentException(string.Format("Default value type does not match type of property '{0}'.", propertyName));
            }

            // An Expression used as default value won't behave as expected since
            //  it doesn't get evaluated.  We explicitly fail it here.
            if (defaultValue is BindingExpression)
            {
                throw new ArgumentException("A BindingExpression object is not a valid default value for a DependencyProperty.");
            }

#if WPF_DISABLED

            if (checkThreadAffinity)
            {
                // If the default value is a DispatcherObject with thread affinity
                // we cannot accept it as a default value. If it implements ISealable
                // we attempt to seal it; if not we throw  an exception. Types not
                // deriving from DispatcherObject are allowed - it is up to the user to
                // make any custom types free-threaded.

                DispatcherObject dispatcherObject = defaultValue as DispatcherObject;

                if (dispatcherObject != null && dispatcherObject.Dispatcher != null)
                {
                    // Try to make the DispatcherObject free-threaded if it's an
                    // ISealable.

                    ISealable valueAsISealable = dispatcherObject as ISealable;

                    if (valueAsISealable != null && valueAsISealable.CanSeal)
                    {
                        Invariant.Assert (!valueAsISealable.IsSealed,
                               "A Sealed ISealable must not have dispatcher affinity");

                        valueAsISealable.Seal();

                        Invariant.Assert(dispatcherObject.Dispatcher == null,
                            "ISealable.Seal() failed after ISealable.CanSeal returned true");
                    }
                    else
                    {
                        throw new ArgumentException(SR.Get(SRID.DefaultValueMustBeFreeThreaded, propertyName));
                    }
                }
            }

#endif

            // After checking for correct type, check default value against
            //  validator (when one is given)
            if (validateValueCallback != null &&
                !validateValueCallback(defaultValue))
            {
                throw new ArgumentException(string.Format("Default value for '{0}' property is not valid because ValidateValueCallback failed.", propertyName));
            }
        }

        /// <summary>
        ///     Parameter validation for OverrideMetadata, includes code to force
        /// all base classes of "forType" to register their metadata so we know
        /// what we are overriding.
        /// </summary>
        private void SetupOverrideMetadata(
                Type forType,
                PropertyMetadata typeMetadata,
            out DependencyObjectType dType,
            out PropertyMetadata baseMetadata)
        {
            if (forType == null)
            {
                throw new ArgumentNullException("forType");
            }

            if (typeMetadata == null)
            {
                throw new ArgumentNullException("typeMetadata");
            }

            if (typeMetadata.Sealed)
            {
                throw new ArgumentException("Metadata is already associated with a type and property. A new one must be created.");
            }

            if (!typeof(DependencyObject).IsAssignableFrom(forType))
            {
                throw new ArgumentException(string.Format("'{0}' type must derive from DependencyObject.", forType.Name));
            }

            // Ensure default value is a correct value (if it was supplied,
            // otherwise, the default value will be taken from the base metadata
            // which was already validated)
            if (typeMetadata.IsDefaultValueModified)
            {
                // Will throw ArgumentException if fails.
                ValidateMetadataDefaultValue(typeMetadata, PropertyType, Name, ValidateValueCallback);
            }

            // Force all base classes to register their metadata
            dType = DependencyObjectType.FromSystemType(forType);

            // Get metadata for the base type
            baseMetadata = GetMetadata(dType.BaseType);

            // Make sure overriding metadata is the same type or derived type of
            // the base metadata
            if (!baseMetadata.GetType().IsAssignableFrom(typeMetadata.GetType()))
            {
                throw new ArgumentException("Metadata override and base metadata must be of the same type or derived type.");
            }
        }

        /// <summary>
        ///     Supply metadata for given type & run static constructors if needed.
        /// </summary>
        /// <remarks>
        ///     The supplied metadata will be merged with the type's base
        ///     metadata
        /// </remarks>
        public void OverrideMetadata(Type forType, PropertyMetadata typeMetadata)
        {
            DependencyObjectType dType;
            PropertyMetadata baseMetadata;

            SetupOverrideMetadata(forType, typeMetadata, out dType, out baseMetadata);

#if WPF_DISABLED

            if (ReadOnly)
            {
                // Readonly and no DependencyPropertyKey - not allowed.
                throw new InvalidOperationException(SR.Get(SRID.ReadOnlyOverrideNotAllowed, Name));
            }

#endif

            ProcessOverrideMetadata(forType, typeMetadata, dType, baseMetadata);
        }

#if WPF_DISABLED

        /// <summary>
        ///     Supply metadata for a given type, overriding a property that is
        /// read-only.  If property is not read only, tells user to use the Plain
        /// Jane OverrideMetadata instead.
        /// </summary>
        public void OverrideMetadata(Type forType, PropertyMetadata typeMetadata, DependencyPropertyKey key)
        {
            DependencyObjectType dType;
            PropertyMetadata baseMetadata;

            SetupOverrideMetadata(forType, typeMetadata, out dType, out baseMetadata);

            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            if (ReadOnly)
            {
                // If the property is read-only, the key must match this property
                //  and the key must match that in the base metadata.

                if (key.DependencyProperty != this)
                {
                    throw new ArgumentException(SR.Get(SRID.ReadOnlyOverrideKeyNotAuthorized, Name));
                }

                VerifyReadOnlyKey(key);
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.PropertyNotReadOnly));
            }

            // Either the property doesn't require a key, or the key match was
            //  successful.  Proceed with the metadata override.
            ProcessOverrideMetadata(forType, typeMetadata, dType, baseMetadata);
        }

#endif

        /// <summary>
        ///     After parameters have been validated for OverrideMetadata, this
        /// method is called to actually update the data structures.
        /// </summary>
        private void ProcessOverrideMetadata(
            Type forType,
            PropertyMetadata typeMetadata,
            DependencyObjectType dType,
            PropertyMetadata baseMetadata)
        {
            // Store per-Type metadata for this property. Locks only on Write.
            // Datastructure guaranteed to be valid for non-locking readers
            lock (Synchronized)
            {
                if (DependencyProperty.UnsetValue == _metadataMap[dType.Id])
                {
                    _metadataMap[dType.Id] = typeMetadata;
                }
                else
                {
                    throw new ArgumentException(string.Format("PropertyMetadata is already registered for type '{0}'.", forType.Name));
                }
            }

            // Merge base's metadata into this metadata
            // CALLBACK
            typeMetadata.InvokeMerge(baseMetadata, this);

            // Type metadata may no longer change (calls OnApply)
            typeMetadata.Seal(this, forType);

            if (typeMetadata.IsInherited)
            {
                _packedData |= Flags.IsPotentiallyInherited;
            }

            if (typeMetadata.DefaultValueWasSet() && (typeMetadata.DefaultValue != DefaultMetadata.DefaultValue))
            {
                _packedData |= Flags.IsDefaultValueChanged;
            }

            if (typeMetadata.UsingDefaultValueFactory)
            {
                _packedData |= Flags.IsPotentiallyUsingDefaultValueFactory;
            }
        }

        internal object GetDefaultValue(DependencyObjectType dependencyObjectType)
        {
            if (!IsDefaultValueChanged)
            {
                return DefaultMetadata.DefaultValue;
            }

            return GetMetadata(dependencyObjectType).DefaultValue;
        }

        internal object GetDefaultValue(Type forType)
        {
            if (!IsDefaultValueChanged)
            {
                return DefaultMetadata.DefaultValue;
            }

            return GetMetadata(DependencyObjectType.FromSystemTypeInternal(forType)).DefaultValue;
        }

        /// <summary>
        ///     Retrieve metadata for a provided type
        /// </summary>
        /// <param name="forType">Type to get metadata</param>
        /// <returns>Property metadata</returns>
        public PropertyMetadata GetMetadata(Type forType)
        {
            if (forType != null)
            {
                return GetMetadata(DependencyObjectType.FromSystemType(forType));
            }
            throw new ArgumentNullException("forType");
        }

        /// <summary>
        ///     Retrieve metadata for a provided DependencyObject
        /// </summary>
        /// <param name="dependencyObject">DependencyObject to get metadata</param>
        /// <returns>Property metadata</returns>
        public PropertyMetadata GetMetadata(DependencyObject dependencyObject)
        {
            if (dependencyObject != null)
            {
                return GetMetadata(dependencyObject.DependencyObjectType);
            }
            throw new ArgumentNullException("dependencyObject");
        }

        /// <summary>
        /// Reteive metadata for a DependencyObject type described by the
        /// given DependencyObjectType
        /// </summary>
        public PropertyMetadata GetMetadata(DependencyObjectType dependencyObjectType)
        {
            // All static constructors for this DType and all base types have already
            // been run. If no overriden metadata was provided, then look up base types.
            // If no metadata found on base types, then return default

            if (null != dependencyObjectType)
            {
                // Do we in fact have any overrides at all?
                int index = _metadataMap.Count - 1;
                int Id;
                object value;

                if (index < 0)
                {
                    // No overrides or it's the base class
                    return _defaultMetadata;
                }
                else if (index == 0)
                {
                    // Only 1 override
                    _metadataMap.GetKeyValuePair(index, out Id, out value);

                    // If there is overriden metadata, then there is a base class with
                    // lower or equal Id of this class, or this class is already a base class
                    // of the overridden one. Therefore dependencyObjectType won't ever
                    // become null before we exit the while loop
                    while (dependencyObjectType.Id > Id)
                    {
                        dependencyObjectType = dependencyObjectType.BaseType;
                    }

                    if (Id == dependencyObjectType.Id)
                    {
                        // Return the override
                        return (PropertyMetadata)value;
                    }
                    // Return default metadata
                }
                else
                {
                    // We have more than 1 override for this class, so we will have to loop through
                    // both the overrides and the class Id
                    if (0 != dependencyObjectType.Id)
                    {
                        do
                        {
                            // Get the Id of the most derived class with overridden metadata
                            _metadataMap.GetKeyValuePair(index, out Id, out value);
                            --index;

                            // If the Id of this class is less than the override, then look for an override
                            // with an equal or lower Id until we run out of overrides
                            while ((dependencyObjectType.Id < Id) && (index >= 0))
                            {
                                _metadataMap.GetKeyValuePair(index, out Id, out value);
                                --index;
                            }

                            // If there is overriden metadata, then there is a base class with
                            // lower or equal Id of this class, or this class is already a base class
                            // of the overridden one. Therefore dependencyObjectType won't ever
                            // become null before we exit the while loop
                            while (dependencyObjectType.Id > Id)
                            {
                                dependencyObjectType = dependencyObjectType.BaseType;
                            }

                            if (Id == dependencyObjectType.Id)
                            {
                                // Return the override
                                return (PropertyMetadata)value;
                            }
                        }
                        while (index >= 0);
                    }
                }
            }
            return _defaultMetadata;
        }

#if WPF_DISABLED

        /// <summary>
        ///     Associate another owner type with this property
        /// </summary>
        /// <remarks>
        ///     The owner type is used when resolving a property by name (<see cref="FromName"/>)
        /// </remarks>
        /// <param name="ownerType">Additional owner type</param>
        /// <returns>This property</returns>
        public DependencyProperty AddOwner(Type ownerType)
        {
            // Forwarding
            return AddOwner(ownerType, null);
        }

        /// <summary>
        ///     Associate another owner type with this property
        /// </summary>
        /// <remarks>
        ///     The owner type is used when resolving a property by name (<see cref="FromName"/>)
        /// </remarks>
        /// <param name="ownerType">Additional owner type</param>
        /// <param name="typeMetadata">Optional type metadata to override on owner's behalf</param>
        /// <returns>This property</returns>
        public DependencyProperty AddOwner(Type ownerType, PropertyMetadata typeMetadata)
        {
            if (ownerType == null)
            {
                throw new ArgumentNullException("ownerType");
            }

            // Map owner type to this property
            // Build key
            FromNameKey key = new FromNameKey(Name, ownerType);

            lock (Synchronized)
            {
                if (PropertyFromName.Contains(key))
                {
                    throw new ArgumentException(SR.Get(SRID.PropertyAlreadyRegistered, Name, ownerType.Name));
                }
            }

            if (typeMetadata != null)
            {
                OverrideMetadata(ownerType, typeMetadata);
            }


            lock (Synchronized)
            {
                PropertyFromName[key] = this;
            }


            return this;
        }

#endif

        /// <summary>
        ///     Name of the property
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        ///     Type of the property
        /// </summary>
        public Type PropertyType
        {
            get { return _propertyType; }
        }

        /// <summary>
        ///     Owning type of the property
        /// </summary>
        public Type OwnerType
        {
            get { return _ownerType; }
        }

        /// <summary>
        ///     Default metadata for the property
        /// </summary>
        public PropertyMetadata DefaultMetadata
        {
            get { return _defaultMetadata; }
        }

        /// <summary>
        ///     Value validation callback
        /// </summary>
        public ValidateValueCallback ValidateValueCallback
        {
            get { return _validateValueCallback; }
        }

        /// <summary>
        ///     Zero-based globally unique index of the property
        /// </summary>
        public int GlobalIndex
        {
            get { return (int)(_packedData & Flags.GlobalIndexMask); }
        }

        internal bool IsObjectType
        {
            get { return (_packedData & Flags.IsObjectType) != 0; }
        }

        internal bool IsValueType
        {
            get { return (_packedData & Flags.IsValueType) != 0; }
        }

#if WPF_DISABLED

        internal bool IsFreezableType
        {
            get { return (_packedData & Flags.IsFreezableType) != 0; }
        }

#endif

        internal bool IsStringType
        {
            get { return (_packedData & Flags.IsStringType) != 0; }
        }

        internal bool IsPotentiallyInherited
        {
            get { return (_packedData & Flags.IsPotentiallyInherited) != 0; }
        }

        internal bool IsDefaultValueChanged
        {
            get { return (_packedData & Flags.IsDefaultValueChanged) != 0; }
        }

        internal bool IsPotentiallyUsingDefaultValueFactory
        {
            get { return (_packedData & Flags.IsPotentiallyUsingDefaultValueFactory) != 0; }
        }

        /// <summary>
        ///     Serves as a hash function for a particular type, suitable for use in
        ///     hashing algorithms and data structures like a hash table
        /// </summary>
        /// <returns>The DependencyProperty's GlobalIndex</returns>
        public override int GetHashCode()
        {
            return GlobalIndex;
        }

        /// <summary>
        ///     Used to determine if given value is appropriate for the type of the property
        /// </summary>
        /// <param name="value">Value to check</param>
        /// <returns>true if value matches property type</returns>
        public bool IsValidType(object value)
        {
            return IsValidType(value, PropertyType);
        }

        /// <summary>
        ///     Used to determine if given value is appropriate for the type of the property
        ///     and the range of values (as specified via the ValidateValueCallback) within that type
        /// </summary>
        /// <param name="value">Value to check</param>
        /// <returns>true if value is appropriate</returns>
        public bool IsValidValue(object value)
        {
            if (!IsValidType(value, PropertyType))
            {
                return false;
            }

            if (ValidateValueCallback != null)
            {
                // CALLBACK
                return ValidateValueCallback(value);
            }

            return true;
        }

#if WPF_DISABLED

        /// <summary>
        ///     Set/Value value disabling
        /// </summary>
        public bool ReadOnly
        {
            get
            {
                return (_readOnlyKey != null);
            }
        }

        /// <summary>
        ///     Returns the DependencyPropertyKey associated with this DP.
        /// </summary>
        internal DependencyPropertyKey DependencyPropertyKey
        {
            get
            {
                return _readOnlyKey;
            }
        }

        internal void VerifyReadOnlyKey( DependencyPropertyKey candidateKey )
        {
            Debug.Assert( ReadOnly, "Why are we trying to validate read-only key on a property that is not read-only?");

            if (_readOnlyKey != candidateKey)
            {
                throw new ArgumentException(SR.Get(SRID.ReadOnlyKeyNotAuthorized));
            }
        }

#endif

        /// <summary>
        ///     Internal version of IsValidValue that bypasses IsValidType check;
        ///     Called from SetValueInternal
        /// </summary>
        /// <param name="value">Value to check</param>
        /// <returns>true if value is appropriate</returns>
        internal bool IsValidValueInternal(object value)
        {
            if (ValidateValueCallback != null)
            {
                // CALLBACK
                return ValidateValueCallback(value);
            }

            return true;
        }

        /// <summary>
        ///     Find a property from name
        /// </summary>
        /// <remarks>
        ///     Search includes base classes of the provided type as well
        /// </remarks>
        /// <param name="name">Name of the property</param>
        /// <param name="ownerType">Owner type of the property</param>
        /// <returns>Dependency property</returns>
        internal static DependencyProperty FromName(string name, Type ownerType)
        {
            DependencyProperty dp = null;

            if (name != null)
            {
                if (ownerType != null)
                {
                    FromNameKey key = new FromNameKey(name, ownerType);

                    while ((dp == null) && (ownerType != null))
                    {
                        // Ensure static constructor of type has run
#if WPF_DISABLED
                        MS.Internal.WindowsBase.SecurityHelper.RunClassConstructor(ownerType);
#else
#if OPENSILVER
                        global::System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(ownerType.TypeHandle);
#else
                        global::System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(ownerType);
#endif
#endif

                        // Locate property
                        key.UpdateNameKey(ownerType);

                        lock (Synchronized)
                        {
                            dp = (DependencyProperty)PropertyFromName[key];
                        }

                        ownerType = ownerType.BaseType;
                    }
                }
                else
                {
                    throw new ArgumentNullException("ownerType");
                }
            }
            else
            {
                throw new ArgumentNullException("name");
            }
            return dp;
        }

        /// <summary>
        ///    String representation
        /// </summary>
        public override string ToString()
        {
            return _name;
        }

        internal static bool IsValidType(object value, Type propertyType)
        {
            if (value == null)
            {
                // Null values are invalid for value-types
                if (propertyType.IsValueType &&
                    !(propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == NullableType))
                {
                    return false;
                }
            }
            else
            {
                // Non-null default value, ensure its the correct type
                if (!propertyType.IsInstanceOfType(value))
                {
                    return false;
                }
            }

            return true;
        }

        private class FromNameKey
        {
            public FromNameKey(string name, Type ownerType)
            {
                _name = name;
                _ownerType = ownerType;

                _hashCode = _name.GetHashCode() ^ _ownerType.GetHashCode();
            }

            public void UpdateNameKey(Type ownerType)
            {
                _ownerType = ownerType;

                _hashCode = _name.GetHashCode() ^ _ownerType.GetHashCode();
            }

            public override int GetHashCode()
            {
                return _hashCode;
            }

            public override bool Equals(object o)
            {
                if ((o != null) && (o is FromNameKey))
                {
                    return Equals((FromNameKey)o);
                }
                else
                {
                    return false;
                }
            }

            public bool Equals(FromNameKey key)
            {
                return (_name.Equals(key._name) && (_ownerType == key._ownerType));
            }

            private string _name;
            private Type _ownerType;

            private int _hashCode;
        }

        private DependencyProperty(string name, Type propertyType, Type ownerType, PropertyMetadata defaultMetadata, ValidateValueCallback validateValueCallback)
        {
            _name = name;
            _propertyType = propertyType;
            _ownerType = ownerType;
            _defaultMetadata = defaultMetadata;
            _validateValueCallback = validateValueCallback;

            Flags packedData;
            lock (Synchronized)
            {
                packedData = (Flags)GetUniqueGlobalIndex(ownerType, name);

                RegisteredPropertyList.Add(this);
            }

            if (propertyType.IsValueType)
            {
                packedData |= Flags.IsValueType;
            }

            if (propertyType == typeof(object))
            {
                packedData |= Flags.IsObjectType;
            }

#if WPF_DISABLED

            if (typeof(Freezable).IsAssignableFrom(propertyType))
            {
                packedData |= Flags.IsFreezableType;
            }

#endif

            if (propertyType == typeof(string))
            {
                packedData |= Flags.IsStringType;
            }

            _packedData = packedData;
        }

        // Synchronized: Covered by DependencyProperty.Synchronized
        internal static int GetUniqueGlobalIndex(Type ownerType, string name)
        {
            // Prevent GlobalIndex from overflow. DependencyProperties are meant to be static members and are to be registered
            // only via static constructors. However there is no cheap way of ensuring this, without having to do a stack walk. Hence
            // concievably people could register DependencyProperties via instance methods and therefore cause the GlobalIndex to
            // overflow. This check will explicitly catch this error, instead of silently malfuntioning.
            if (GlobalIndexCount >= (int)Flags.GlobalIndexMask)
            {
                if (ownerType != null)
                {
                    throw new InvalidOperationException(string.Format("DependencyProperty limit has been exceeded upon registration of '{0}'. Dependency properties are normally static class members registered with static field initializers or static constructors. In this case, there may be dependency properties accidentally getting initialized in instance constructors and thus causing the maximum limit to be exceeded.", ownerType.Name + "." + name));
                }
                else
                {
                    throw new InvalidOperationException(string.Format("DependencyProperty limit has been exceeded upon registration of '{0}'. Dependency properties are normally static class members registered with static field initializers or static constructors. In this case, there may be dependency properties accidentally getting initialized in instance constructors and thus causing the maximum limit to be exceeded.", "ConstantProperty"));
                }
            }

            // Covered by Synchronized by caller
            return GlobalIndexCount++;
        }

#if WPF_DISABLED

        /// <summary>
        /// This is the callback designers use to participate in the computation of property
        /// values at design time. Eg. Even if the author sets Visibility to Hidden, the designer
        /// wants to coerce the value to Visible at design time so that the element doesn't
        /// disappear from the design surface.
        /// </summary>
        internal CoerceValueCallback DesignerCoerceValueCallback
        {
            get {  return _designerCoerceValueCallback; }
            set
            {
                if (ReadOnly)
                {
                    throw new InvalidOperationException(SR.Get(SRID.ReadOnlyDesignerCoersionNotAllowed, Name));
                }

                _designerCoerceValueCallback = value;
            }
        }

#endif

        /// <summary> Standard unset value </summary>
        public static readonly object UnsetValue = new NamedObject("DependencyProperty.UnsetValue");

        private string _name;
        private Type _propertyType;
        private Type _ownerType;
        private PropertyMetadata _defaultMetadata;
        private ValidateValueCallback _validateValueCallback;

#if WPF_DISABLED

        private DependencyPropertyKey _readOnlyKey;

#endif

        [Flags]
        private enum Flags : int
        {
            GlobalIndexMask = 0x0000FFFF,
            IsValueType = 0x00010000,
#if WPF_DISABLED
            IsFreezableType = 0x00020000,
#endif
            IsStringType = 0x00040000,
            IsPotentiallyInherited = 0x00080000,
            IsDefaultValueChanged = 0x00100000,
            IsPotentiallyUsingDefaultValueFactory = 0x00200000,
            IsObjectType = 0x00400000,
            // 0xFF800000   free bits
        }

        private Flags _packedData;

        // Synchronized (write locks, lock-free reads): Covered by DependencyProperty instance
        // This is a map that contains the IDs of derived classes that have overriden metadata
        /* property */
        internal InsertionSortMap _metadataMap = new InsertionSortMap();

#if WPF_DISABLED

        private CoerceValueCallback _designerCoerceValueCallback;

#endif

        // Synchronized (write locks, lock-free reads): Covered by DependencyProperty.Synchronized
        /* property */
        internal static ItemStructList<DependencyProperty> RegisteredPropertyList = new ItemStructList<DependencyProperty>(768);

        // Synchronized: Covered by DependencyProperty.Synchronized
        private static Hashtable PropertyFromName = new Hashtable();

        // Synchronized: Covered by DependencyProperty.Synchronized
        private static int GlobalIndexCount;

        // Global, cross-object synchronization
        internal static object Synchronized = new object();

        // Nullable Type
        private static Type NullableType = typeof(Nullable<>);

        /// <summary>
        ///     Returns the number of all registered properties.
        /// </summary>
        internal static int RegisteredPropertyCount
        {
            get
            {
                return RegisteredPropertyList.Count;
            }
        }

        /// <summary>
        ///     Returns an enumeration of properties that are
        ///     currently registered.
        ///     Synchronized (write locks, lock-free reads): Covered by DependencyProperty.Synchronized
        /// </summary>
        internal static IEnumerable RegisteredProperties
        {
            get
            {
                foreach (DependencyProperty dp in RegisteredPropertyList.List)
                {
                    if (dp != null)
                    {
                        yield return dp;
                    }
                }
            }
        }

#region CSHTML5 Internal

        public bool IsAttached { get; private set; }

#endregion
    }
}
