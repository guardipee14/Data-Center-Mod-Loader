using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DCML.Core.Abstractions;
using DCML.Core.Models;

namespace DCML.Loader.MelonLoader;

internal sealed class MelonGameComponentStateReader :
    IDCMLGameComponentStateReader
{
    private readonly IDCMLGameThread _gameThread;

    private readonly Lazy<UnityReflection> _unityReflection =
        new Lazy<UnityReflection>(
            UnityReflection.Create,
            true);

    public MelonGameComponentStateReader(
        IDCMLGameThread gameThread)
    {
        _gameThread =
            gameThread ??
            throw new ArgumentNullException(
                nameof(gameThread));
    }

    public Task<IReadOnlyList<DCMLGameComponentState>> ReadAsync(
        DCMLGameComponentStateQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(
                nameof(query));
        }

        return
            _gameThread.InvokeAsync<IReadOnlyList<DCMLGameComponentState>>(
                () =>
                    ReadOnMainThread(
                        query));
    }

    private IReadOnlyList<DCMLGameComponentState> ReadOnMainThread(
        DCMLGameComponentStateQuery query)
    {
        if (!_gameThread.IsMainThread)
        {
            throw new InvalidOperationException(
                "Game component state may only be read from the captured game thread.");
        }

        var matches =
            new List<DCMLGameComponentState>();

        foreach (
            object gameObject in
            _unityReflection.Value.FindAllGameObjects())
        {
            try
            {
                matches.AddRange(
                    _unityReflection.Value.ReadMatchingComponents(
                        gameObject,
                        query));
            }
            catch
            {
                // A destroyed/unusual Unity object must not invalidate
                // an otherwise read-only snapshot.
            }
        }

        return
            matches
                .OrderBy(value => value.IsResource)
                .ThenBy(
                    value => value.SceneName,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    value => value.HierarchyPath,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    value => value.Name,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(value => value.InstanceId)
                .ThenBy(value => value.ComponentInstanceId)
                .Skip(query.SkipResults)
                .Take(query.MaxResults)
                .ToArray();
    }

    private sealed class UnityReflection
    {
        private readonly Type _gameObjectType;
        private readonly Type _componentType;
        private readonly PropertyInfo _objectNameProperty;
        private readonly MethodInfo _getInstanceIdMethod;
        private readonly PropertyInfo _activeInHierarchyProperty;
        private readonly PropertyInfo _sceneProperty;
        private readonly PropertyInfo _transformProperty;
        private readonly PropertyInfo _transformParentProperty;
        private readonly PropertyInfo _sceneNameProperty;
        private readonly PropertyInfo _sceneIsLoadedProperty;
        private readonly MethodInfo _sceneIsValidMethod;
        private readonly MethodInfo _findObjectsOfTypeAllMethod;
        private readonly MethodInfo _getComponentsMethod;

        private UnityReflection(
            Type gameObjectType,
            Type componentType,
            PropertyInfo objectNameProperty,
            MethodInfo getInstanceIdMethod,
            PropertyInfo activeInHierarchyProperty,
            PropertyInfo sceneProperty,
            PropertyInfo transformProperty,
            PropertyInfo transformParentProperty,
            PropertyInfo sceneNameProperty,
            PropertyInfo sceneIsLoadedProperty,
            MethodInfo sceneIsValidMethod,
            MethodInfo findObjectsOfTypeAllMethod,
            MethodInfo getComponentsMethod)
        {
            _gameObjectType = gameObjectType;
            _componentType = componentType;
            _objectNameProperty = objectNameProperty;
            _getInstanceIdMethod = getInstanceIdMethod;
            _activeInHierarchyProperty = activeInHierarchyProperty;
            _sceneProperty = sceneProperty;
            _transformProperty = transformProperty;
            _transformParentProperty = transformParentProperty;
            _sceneNameProperty = sceneNameProperty;
            _sceneIsLoadedProperty = sceneIsLoadedProperty;
            _sceneIsValidMethod = sceneIsValidMethod;
            _findObjectsOfTypeAllMethod = findObjectsOfTypeAllMethod;
            _getComponentsMethod = getComponentsMethod;
        }

        public static UnityReflection Create()
        {
            Type unityObjectType =
                FindLoadedType(
                    "UnityEngine.Object");

            Type gameObjectType =
                FindLoadedType(
                    "UnityEngine.GameObject");

            Type componentType =
                FindLoadedType(
                    "UnityEngine.Component");

            Type transformType =
                FindLoadedType(
                    "UnityEngine.Transform");

            Type resourcesType =
                FindLoadedType(
                    "UnityEngine.Resources");

            Type sceneType =
                FindLoadedType(
                    "UnityEngine.SceneManagement.Scene");

            MethodInfo findObjectsDefinition =
                RequireMethod(
                    resourcesType,
                    "FindObjectsOfTypeAll",
                    true,
                    0,
                    true);

            MethodInfo getComponentsDefinition =
                RequireMethod(
                    gameObjectType,
                    "GetComponents",
                    false,
                    0,
                    true);

            return
                new UnityReflection(
                    gameObjectType,
                    componentType,
                    RequireProperty(
                        unityObjectType,
                        "name"),
                    RequireMethod(
                        unityObjectType,
                        "GetInstanceID",
                        false,
                        0,
                        false),
                    RequireProperty(
                        gameObjectType,
                        "activeInHierarchy"),
                    RequireProperty(
                        gameObjectType,
                        "scene"),
                    RequireProperty(
                        gameObjectType,
                        "transform"),
                    RequireProperty(
                        transformType,
                        "parent"),
                    RequireProperty(
                        sceneType,
                        "name"),
                    RequireProperty(
                        sceneType,
                        "isLoaded"),
                    RequireMethod(
                        sceneType,
                        "IsValid",
                        false,
                        0,
                        false),
                    findObjectsDefinition.MakeGenericMethod(
                        gameObjectType),
                    getComponentsDefinition.MakeGenericMethod(
                        componentType));
        }

        public IEnumerable<object> FindAllGameObjects()
        {
            object result =
                _findObjectsOfTypeAllMethod.Invoke(
                    null,
                    null);

            foreach (
                object value in
                Enumerate(
                    result))
            {
                if (
                    value is not null &&
                    _gameObjectType.IsInstanceOfType(
                        value))
                {
                    yield return value;
                }
            }
        }

        public IEnumerable<DCMLGameComponentState> ReadMatchingComponents(
            object gameObject,
            DCMLGameComponentStateQuery query)
        {
            object scene =
                _sceneProperty.GetValue(
                    gameObject);

            bool sceneIsValid =
                scene is not null &&
                Convert.ToBoolean(
                    _sceneIsValidMethod.Invoke(
                        scene,
                        null),
                    CultureInfo.InvariantCulture);

            bool sceneIsLoaded =
                sceneIsValid &&
                Convert.ToBoolean(
                    _sceneIsLoadedProperty.GetValue(
                        scene),
                    CultureInfo.InvariantCulture);

            bool isResource =
                !sceneIsValid ||
                !sceneIsLoaded;

            if (
                query.Scope ==
                    DCMLGameComponentScope.Scene &&
                isResource)
            {
                yield break;
            }

            if (
                query.Scope ==
                    DCMLGameComponentScope.Resource &&
                !isResource)
            {
                yield break;
            }

            string sceneName =
                isResource
                    ? string.Empty
                    : Convert.ToString(
                        _sceneNameProperty.GetValue(
                            scene),
                        CultureInfo.InvariantCulture)
                        ?? string.Empty;

            if (
                !isResource &&
                query.SceneName.Length > 0 &&
                !string.Equals(
                    sceneName,
                    query.SceneName,
                    StringComparison.OrdinalIgnoreCase))
            {
                yield break;
            }

            bool? activeInHierarchy =
                null;

            if (!isResource)
            {
                activeInHierarchy =
                    Convert.ToBoolean(
                        _activeInHierarchyProperty.GetValue(
                            gameObject),
                        CultureInfo.InvariantCulture);

                if (
                    !query.IncludeInactive &&
                    activeInHierarchy == false)
                {
                    yield break;
                }
            }

            string name =
                Convert.ToString(
                    _objectNameProperty.GetValue(
                        gameObject),
                    CultureInfo.InvariantCulture)
                ?? string.Empty;

            int instanceId =
                Convert.ToInt32(
                    _getInstanceIdMethod.Invoke(
                        gameObject,
                        null),
                    CultureInfo.InvariantCulture);

            if (
                query.GameObjectInstanceIds.Count > 0 &&
                !query.GameObjectInstanceIds.Contains(
                    instanceId)
            )
            {
                yield break;
            }

            string hierarchyPath =
                isResource
                    ? name
                    : CreateHierarchyPath(
                        _transformProperty.GetValue(
                            gameObject),
                        name);

            object components =
                _getComponentsMethod.Invoke(
                    gameObject,
                    null);

            foreach (
                object component in
                Enumerate(
                    components))
            {
                if (
                    component is null ||
                    !_componentType.IsInstanceOfType(
                        component))
                {
                    continue;
                }

                string componentTypeName =
                    GetComponentTypeName(
                        component);

                if (
                    !TypeNameMatches(
                        componentTypeName,
                        query.ComponentTypeName))
                {
                    continue;
                }

                int componentInstanceId =
                    Convert.ToInt32(
                        _getInstanceIdMethod.Invoke(
                            component,
                            null),
                        CultureInfo.InvariantCulture);

                if (
                    query.ComponentInstanceIds.Count > 0 &&
                    !query.ComponentInstanceIds.Contains(
                        componentInstanceId)
                )
                {
                    continue;
                }

                object readableComponent =
                    NativeIl2CppComponentWrapper.TryCreate(
                        component,
                        componentTypeName)
                    ?? component;

                var values =
                    new List<KeyValuePair<string, DCMLGameValue>>();

                foreach (
                    string memberName in
                    query.MemberNames)
                {
                    values.Add(
                        new KeyValuePair<string, DCMLGameValue>(
                            memberName,
                            ReadMember(
                                readableComponent,
                                memberName)));
                }

                yield return
                    new DCMLGameComponentState(
                        instanceId,
                        name,
                        sceneName,
                        hierarchyPath,
                        activeInHierarchy,
                        isResource,
                        componentTypeName,
                        values,
                        componentInstanceId);
            }
        }

        private string CreateHierarchyPath(
            object transform,
            string fallbackName)
        {
            if (transform is null)
            {
                return fallbackName;
            }

            var names =
                new List<string>();

            object current =
                transform;

            for (
                int depth = 0;
                depth < 128 &&
                current is not null;
                depth++)
            {
                string currentName =
                    Convert.ToString(
                        _objectNameProperty.GetValue(
                            current),
                        CultureInfo.InvariantCulture)
                    ?? string.Empty;

                if (currentName.Length > 0)
                {
                    names.Add(
                        currentName);
                }

                object parent =
                    _transformParentProperty.GetValue(
                        current);

                if (
                    parent is null ||
                    ReferenceEquals(
                        parent,
                        current))
                {
                    break;
                }

                current =
                    parent;
            }

            if (names.Count == 0)
            {
                return fallbackName;
            }

            names.Reverse();

            return
                string.Join(
                    "/",
                    names);
        }

        private static DCMLGameValue ReadMember(
            object component,
            string memberName)
        {
            try
            {
                Type runtimeType =
                    component.GetType();

                PropertyInfo property =
                    runtimeType
                        .GetProperties(
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.Instance)
                        .FirstOrDefault(
                            value =>
                                value.GetIndexParameters().Length == 0 &&
                                string.Equals(
                                    value.Name,
                                    memberName,
                                    StringComparison.OrdinalIgnoreCase));

                if (property is not null)
                {
                    if (
                        property.GetGetMethod(
                            true) is null)
                    {
                        return
                            new DCMLGameValue(
                                DCMLGameValueKind.Unavailable,
                                property.PropertyType.FullName,
                                diagnostic:
                                    "Property has no getter.");
                    }

                    return
                        NormalizeValue(
                            property.PropertyType,
                            property.GetValue(
                                component));
                }

                FieldInfo field =
                    runtimeType
                        .GetFields(
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.Instance)
                        .FirstOrDefault(
                            value =>
                                string.Equals(
                                    value.Name,
                                    memberName,
                                    StringComparison.OrdinalIgnoreCase));

                if (field is not null)
                {
                    return
                        NormalizeValue(
                            field.FieldType,
                            field.GetValue(
                                component));
                }

                return
                    new DCMLGameValue(
                        DCMLGameValueKind.Unavailable,
                        diagnostic:
                            "Member was not found.");
            }
            catch (Exception exception)
            {
                return
                    new DCMLGameValue(
                        DCMLGameValueKind.Unavailable,
                        diagnostic:
                            exception.GetType().FullName +
                            ": " +
                            exception.Message);
            }
        }

        private static DCMLGameValue NormalizeValue(
            Type declaredType,
            object value)
        {
            string typeName =
                declaredType.FullName ??
                declaredType.Name;

            if (value is null)
            {
                return
                    new DCMLGameValue(
                        DCMLGameValueKind.Null,
                        typeName);
            }

            Type runtimeType =
                value.GetType();

            if (value is string text)
            {
                return
                    new DCMLGameValue(
                        DCMLGameValueKind.String,
                        typeName,
                        stringValue:
                            text);
            }

            if (value is bool boolean)
            {
                return
                    new DCMLGameValue(
                        DCMLGameValueKind.Boolean,
                        typeName,
                        booleanValue:
                            boolean);
            }

            DCMLGameReference? reference =
                UnityObjectReferenceReader.TryCreate(
                    value);

            if (reference is not null)
            {
                return
                    new DCMLGameValue(
                        DCMLGameValueKind.Reference,
                        typeName,
                        referenceValue:
                            reference);
            }

            DCMLGameValue? referenceCollection =
                TryNormalizeUnityReferenceCollection(
                    declaredType,
                    value);

            if (referenceCollection is not null)
            {
                return
                    referenceCollection;
            }

            if (runtimeType.IsEnum)
            {
                return
                    new DCMLGameValue(
                        DCMLGameValueKind.Enum,
                        typeName,
                        stringValue:
                            value.ToString());
            }

            switch (
                Type.GetTypeCode(
                    runtimeType))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                    return
                        new DCMLGameValue(
                            DCMLGameValueKind.Integer,
                            typeName,
                            integerValue:
                                Convert.ToInt64(
                                    value,
                                    CultureInfo.InvariantCulture));

                case TypeCode.UInt64:
                    ulong unsigned =
                        Convert.ToUInt64(
                            value,
                            CultureInfo.InvariantCulture);

                    if (unsigned <= long.MaxValue)
                    {
                        return
                            new DCMLGameValue(
                                DCMLGameValueKind.Integer,
                                typeName,
                                integerValue:
                                    (long) unsigned);
                    }

                    return
                        new DCMLGameValue(
                            DCMLGameValueKind.Unsupported,
                            typeName,
                            diagnostic:
                                unsigned.ToString(
                                    CultureInfo.InvariantCulture));

                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return
                        new DCMLGameValue(
                            DCMLGameValueKind.Number,
                            typeName,
                            numberValue:
                                Convert.ToDouble(
                                    value,
                                    CultureInfo.InvariantCulture));
            }

            return
                new DCMLGameValue(
                    DCMLGameValueKind.Unsupported,
                    typeName,
                    diagnostic:
                        runtimeType.FullName ??
                        runtimeType.Name);
        }

        private static DCMLGameValue? TryNormalizeUnityReferenceCollection(
            Type declaredType,
            object value)
        {
            Type runtimeType =
                value.GetType();

            string runtimeTypeName =
                runtimeType.FullName ??
                runtimeType.Name;

            const string referenceArrayPrefix =
                "Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray`1";

            if (
                !runtimeTypeName.StartsWith(
                    referenceArrayPrefix,
                    StringComparison.Ordinal)
            )
            {
                return null;
            }

            const int maximumReferences =
                256;

            int? collectionCount =
                TryGetCollectionLength(
                    value);

            var references =
                new List<DCMLGameReference>();

            foreach (
                object item in
                Enumerate(
                    value))
            {
                if (
                    references.Count >=
                        maximumReferences)
                {
                    break;
                }

                DCMLGameReference? reference =
                    UnityObjectReferenceReader.TryCreate(
                        item);

                if (reference is not null)
                {
                    references.Add(
                        reference);
                }
            }

            string declaredTypeName =
                declaredType.FullName ??
                declaredType.Name;

            string diagnostic =
                collectionCount.HasValue &&
                collectionCount.Value >
                    maximumReferences
                    ? "Reference collection was truncated to " +
                        maximumReferences +
                        " item(s) from " +
                        collectionCount.Value +
                        "."
                    : string.Empty;

            return
                new DCMLGameValue(
                    DCMLGameValueKind.ReferenceCollection,
                    declaredTypeName,
                    diagnostic:
                        diagnostic,
                    referenceValues:
                        references,
                    collectionCount:
                        collectionCount);
        }

        private static int? TryGetCollectionLength(
            object value)
        {
            try
            {
                PropertyInfo lengthProperty =
                    value
                        .GetType()
                        .GetProperty(
                            "Length",
                            BindingFlags.Public |
                            BindingFlags.Instance);

                if (lengthProperty is null)
                {
                    return null;
                }

                return
                    Convert.ToInt32(
                        lengthProperty.GetValue(
                            value),
                        CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }

        private static bool TypeNameMatches(
            string actualTypeName,
            string requestedTypeName)
        {
            if (
                string.Equals(
                    actualTypeName,
                    requestedTypeName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string actualSimple =
                actualTypeName.Substring(
                    actualTypeName.LastIndexOf('.') + 1);

            string requestedSimple =
                requestedTypeName.Substring(
                    requestedTypeName.LastIndexOf('.') + 1);

            return
                string.Equals(
                    actualSimple,
                    requestedSimple,
                    StringComparison.OrdinalIgnoreCase);
        }

        private static string GetComponentTypeName(
            object component)
        {
            string nativeTypeName =
                NativeIl2CppTypeNameResolver.TryGetTypeName(
                    component);

            if (
                !string.IsNullOrWhiteSpace(
                    nativeTypeName))
            {
                return nativeTypeName;
            }

            Type runtimeType =
                component.GetType();

            return
                runtimeType.FullName ??
                runtimeType.Name;
        }

        private static IEnumerable<object> Enumerate(
            object value)
        {
            if (value is null)
            {
                yield break;
            }

            if (value is IEnumerable enumerable)
            {
                foreach (
                    object item in
                    enumerable)
                {
                    if (item is not null)
                    {
                        yield return item;
                    }
                }

                yield break;
            }

            Type type =
                value.GetType();

            PropertyInfo lengthProperty =
                type.GetProperty(
                    "Length",
                    BindingFlags.Public |
                    BindingFlags.Instance);

            PropertyInfo itemProperty =
                type.GetProperty(
                    "Item",
                    BindingFlags.Public |
                    BindingFlags.Instance);

            if (
                lengthProperty is null ||
                itemProperty is null)
            {
                yield break;
            }

            int length =
                Convert.ToInt32(
                    lengthProperty.GetValue(
                        value),
                    CultureInfo.InvariantCulture);

            for (
                int index = 0;
                index < length;
                index++)
            {
                object item =
                    itemProperty.GetValue(
                        value,
                        new object[]
                        {
                            index
                        });

                if (item is not null)
                {
                    yield return item;
                }
            }
        }

        private static Type FindLoadedType(
            string fullName)
        {
            foreach (
                Assembly assembly in
                AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type =
                    assembly.GetType(
                        fullName,
                        false,
                        false);

                if (type is not null)
                {
                    return type;
                }
            }

            throw new InvalidOperationException(
                $"Required Unity/IL2CPP type '{fullName}' is not loaded.");
        }

        private static PropertyInfo RequireProperty(
            Type type,
            string name)
        {
            return
                type.GetProperty(
                    name,
                    BindingFlags.Public |
                    BindingFlags.Instance |
                    BindingFlags.Static)
                ?? throw new MissingMemberException(
                    type.FullName,
                    name);
        }

        private static MethodInfo RequireMethod(
            Type type,
            string name,
            bool isStatic,
            int parameterCount,
            bool genericDefinition)
        {
            return
                type
                    .GetMethods(
                        BindingFlags.Public |
                        BindingFlags.Instance |
                        BindingFlags.Static)
                    .FirstOrDefault(
                        value =>
                            value.Name == name &&
                            value.IsStatic == isStatic &&
                            value.GetParameters().Length ==
                                parameterCount &&
                            value.IsGenericMethodDefinition ==
                                genericDefinition)
                ?? throw new MissingMethodException(
                    type.FullName,
                    name);
        }

        private static class UnityObjectReferenceReader
        {
            private static readonly Lazy<Reader>
                ReaderInstance =
                    new Lazy<Reader>(
                        CreateReader,
                        true);

            public static DCMLGameReference? TryCreate(
                object value)
            {
                try
                {
                    Reader reader =
                        ReaderInstance.Value;

                    if (
                        reader is null ||
                        !reader.UnityObjectType.IsInstanceOfType(
                            value)
                    )
                    {
                        return null;
                    }

                    string name =
                        Convert.ToString(
                            reader.NameProperty.GetValue(
                                value),
                            CultureInfo.InvariantCulture)
                        ?? string.Empty;

                    int instanceId =
                        Convert.ToInt32(
                            reader.GetInstanceIdMethod.Invoke(
                                value,
                                null),
                            CultureInfo.InvariantCulture);

                    string nativeTypeName =
                        NativeIl2CppTypeNameResolver.TryGetTypeName(
                            value);

                    if (
                        string.IsNullOrWhiteSpace(
                            nativeTypeName)
                    )
                    {
                        Type runtimeType =
                            value.GetType();

                        nativeTypeName =
                            runtimeType.FullName ??
                            runtimeType.Name;
                    }

                    return
                        new DCMLGameReference(
                            instanceId,
                            name,
                            nativeTypeName);
                }
                catch
                {
                    return null;
                }
            }

            private static Reader CreateReader()
            {
                try
                {
                    Type unityObjectType =
                        FindLoadedType(
                            "UnityEngine.Object");

                    PropertyInfo nameProperty =
                        unityObjectType.GetProperty(
                            "name",
                            BindingFlags.Public |
                            BindingFlags.Instance);

                    MethodInfo getInstanceIdMethod =
                        unityObjectType.GetMethod(
                            "GetInstanceID",
                            BindingFlags.Public |
                            BindingFlags.Instance,
                            binder:
                                null,
                            types:
                                Type.EmptyTypes,
                            modifiers:
                                null);

                    if (
                        nameProperty is null ||
                        getInstanceIdMethod is null
                    )
                    {
                        return null;
                    }

                    return
                        new Reader(
                            unityObjectType,
                            nameProperty,
                            getInstanceIdMethod);
                }
                catch
                {
                    return null;
                }
            }

            private sealed class Reader
            {
                public Reader(
                    Type unityObjectType,
                    PropertyInfo nameProperty,
                    MethodInfo getInstanceIdMethod)
                {
                    UnityObjectType =
                        unityObjectType;

                    NameProperty =
                        nameProperty;

                    GetInstanceIdMethod =
                        getInstanceIdMethod;
                }

                public Type UnityObjectType { get; }

                public PropertyInfo NameProperty { get; }

                public MethodInfo GetInstanceIdMethod { get; }
            }
        }

        private static class NativeIl2CppComponentWrapper
        {
            private static readonly Lazy<PropertyInfo>
                PointerProperty =
                    new Lazy<PropertyInfo>(
                        () =>
                        {
                            Type objectBaseType =
                                FindLoadedType(
                                    "Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase");

                            PropertyInfo property =
                                objectBaseType.GetProperty(
                                    "Pointer",
                                    BindingFlags.Public |
                                    BindingFlags.Instance);

                            return
                                property ??
                                throw new MissingMemberException(
                                    objectBaseType.FullName,
                                    "Pointer");
                        },
                        true);

            public static object TryCreate(
                object component,
                string nativeTypeName)
            {
                try
                {
                    Type wrapperType =
                        FindLoadedType(
                            nativeTypeName);

                    if (
                        wrapperType.IsInstanceOfType(
                            component)
                    )
                    {
                        return component;
                    }

                    object pointerValue =
                        PointerProperty.Value.GetValue(
                            component);

                    if (
                        pointerValue is not IntPtr
                    )
                    {
                        return null;
                    }

                    IntPtr pointer =
                        (IntPtr) pointerValue;

                    if (
                        pointer ==
                        IntPtr.Zero
                    )
                    {
                        return null;
                    }

                    ConstructorInfo constructor =
                        wrapperType.GetConstructor(
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.Instance,
                            binder:
                                null,
                            types:
                                new[]
                                {
                                    typeof(IntPtr)
                                },
                            modifiers:
                                null);

                    if (constructor is null)
                    {
                        return null;
                    }

                    return
                        constructor.Invoke(
                            new object[]
                            {
                                pointer
                            });
                }
                catch
                {
                    return null;
                }
            }
        }

        private static class NativeIl2CppTypeNameResolver
        {
            private static readonly Lazy<Resolver> ResolverInstance =
                new Lazy<Resolver>(
                    CreateResolver,
                    true);

            public static string TryGetTypeName(
                object component)
            {
                try
                {
                    Resolver resolver =
                        ResolverInstance.Value;

                    return
                        resolver is null
                            ? string.Empty
                            : resolver.GetTypeName(
                                component);
                }
                catch
                {
                    return string.Empty;
                }
            }

            private static Resolver CreateResolver()
            {
                try
                {
                    Type objectBaseType =
                        FindLoadedType(
                            "Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase");

                    Type il2CppType =
                        FindLoadedType(
                            "Il2CppInterop.Runtime.IL2CPP");

                    PropertyInfo objectClassProperty =
                        objectBaseType.GetProperty(
                            "ObjectClass",
                            BindingFlags.Public |
                            BindingFlags.Instance);

                    MethodInfo getClassNameMethod =
                        il2CppType.GetMethod(
                            "il2cpp_class_get_name_",
                            BindingFlags.Public |
                            BindingFlags.Static);

                    MethodInfo getClassNamespaceMethod =
                        il2CppType.GetMethod(
                            "il2cpp_class_get_namespace_",
                            BindingFlags.Public |
                            BindingFlags.Static);

                    if (
                        objectClassProperty is null ||
                        getClassNameMethod is null ||
                        getClassNamespaceMethod is null)
                    {
                        return null;
                    }

                    return
                        new Resolver(
                            objectClassProperty,
                            getClassNameMethod,
                            getClassNamespaceMethod);
                }
                catch
                {
                    return null;
                }
            }

            private sealed class Resolver
            {
                private readonly PropertyInfo _objectClassProperty;
                private readonly MethodInfo _getClassNameMethod;
                private readonly MethodInfo _getClassNamespaceMethod;

                public Resolver(
                    PropertyInfo objectClassProperty,
                    MethodInfo getClassNameMethod,
                    MethodInfo getClassNamespaceMethod)
                {
                    _objectClassProperty = objectClassProperty;
                    _getClassNameMethod = getClassNameMethod;
                    _getClassNamespaceMethod = getClassNamespaceMethod;
                }

                public string GetTypeName(
                    object component)
                {
                    object classValue =
                        _objectClassProperty.GetValue(
                            component);

                    if (
                        classValue is not IntPtr ||
                        (IntPtr) classValue == IntPtr.Zero)
                    {
                        return string.Empty;
                    }

                    IntPtr classPointer =
                        (IntPtr) classValue;

                    string className =
                        Convert.ToString(
                            _getClassNameMethod.Invoke(
                                null,
                                new object[]
                                {
                                    classPointer
                                }),
                            CultureInfo.InvariantCulture)
                        ?? string.Empty;

                    string classNamespace =
                        Convert.ToString(
                            _getClassNamespaceMethod.Invoke(
                                null,
                                new object[]
                                {
                                    classPointer
                                }),
                            CultureInfo.InvariantCulture)
                        ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(className))
                    {
                        return string.Empty;
                    }

                    className = className.Trim();
                    classNamespace = classNamespace.Trim();

                    if (
                        classNamespace.StartsWith(
                            "UnityEngine",
                            StringComparison.Ordinal) ||
                        string.Equals(
                            classNamespace,
                            "TMPro",
                            StringComparison.Ordinal))
                    {
                        return
                            classNamespace +
                            "." +
                            className;
                    }

                    return
                        classNamespace.Length == 0
                            ? "Il2Cpp." + className
                            : "Il2Cpp." + classNamespace + "." + className;
                }
            }
        }
    }
}
