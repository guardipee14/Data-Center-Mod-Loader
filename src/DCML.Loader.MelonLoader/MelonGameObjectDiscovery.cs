using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DCML.Core.Abstractions;
using DCML.Core.Models;

namespace DCML.Loader.MelonLoader;

internal sealed class MelonGameObjectDiscovery :
    IDCMLGameObjectDiscovery
{
    private readonly Lazy<UnityReflection>
        _unityReflection =
            new Lazy<UnityReflection>(
                UnityReflection.Create,
                true);

    public IReadOnlyList<DCMLGameObjectInfo> Find(
        DCMLGameObjectQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(
                nameof(query));
        }

        UnityReflection unity =
            _unityReflection.Value;

        var matches =
            new List<DCMLGameObjectInfo>();

        foreach (
            object gameObject in
            unity.FindAllGameObjects()
        )
        {
            DCMLGameObjectInfo info;

            try
            {
                if (
                    !unity.TryCreateInfo(
                        gameObject,
                        out info)
                )
                {
                    continue;
                }
            }
            catch
            {
                // One destroyed or unusual Unity object should
                // never invalidate an otherwise read-only snapshot.
                continue;
            }

            if (
                !query.IncludeInactive &&
                !info.ActiveInHierarchy
            )
            {
                continue;
            }

            if (
                query.NameContains.Length > 0 &&
                info.Name.IndexOf(
                    query.NameContains,
                    StringComparison.OrdinalIgnoreCase) < 0
            )
            {
                continue;
            }

            if (
                query.SceneName.Length > 0 &&
                !string.Equals(
                    info.SceneName,
                    query.SceneName,
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                continue;
            }

            if (
                query.ComponentTypeName.Length > 0 &&
                !HasComponentType(
                    info.ComponentTypeNames,
                    query.ComponentTypeName)
            )
            {
                continue;
            }

            if (
                query.ComponentTypeNamePrefix.Length > 0 &&
                !HasComponentTypePrefix(
                    info.ComponentTypeNames,
                    query.ComponentTypeNamePrefix)
            )
            {
                continue;
            }

            matches.Add(
                info);
        }

        return
            matches
                .OrderBy(
                    value =>
                        value.SceneName,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    value =>
                        value.HierarchyPath,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    value =>
                        value.InstanceId)
                .Take(
                    query.MaxResults)
                .ToArray();
    }

    private static bool HasComponentType(
        IReadOnlyList<string> componentTypeNames,
        string requestedTypeName)
    {
        foreach (
            string componentTypeName in
            componentTypeNames
        )
        {
            if (
                string.Equals(
                    componentTypeName,
                    requestedTypeName,
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                return true;
            }

            int lastDot =
                componentTypeName.LastIndexOf('.');

            string simpleName =
                lastDot >= 0
                    ? componentTypeName.Substring(
                        lastDot + 1)
                    : componentTypeName;

            if (
                string.Equals(
                    simpleName,
                    requestedTypeName,
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasComponentTypePrefix(
        IReadOnlyList<string> componentTypeNames,
        string requestedPrefix)
    {
        foreach (
            string componentTypeName in
            componentTypeNames
        )
        {
            if (
                componentTypeName.StartsWith(
                    requestedPrefix,
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                return true;
            }
        }

        return false;
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
            _gameObjectType =
                gameObjectType;

            _componentType =
                componentType;

            _objectNameProperty =
                objectNameProperty;

            _getInstanceIdMethod =
                getInstanceIdMethod;

            _activeInHierarchyProperty =
                activeInHierarchyProperty;

            _sceneProperty =
                sceneProperty;

            _transformProperty =
                transformProperty;

            _transformParentProperty =
                transformParentProperty;

            _sceneNameProperty =
                sceneNameProperty;

            _sceneIsLoadedProperty =
                sceneIsLoadedProperty;

            _sceneIsValidMethod =
                sceneIsValidMethod;

            _findObjectsOfTypeAllMethod =
                findObjectsOfTypeAllMethod;

            _getComponentsMethod =
                getComponentsMethod;
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

            PropertyInfo objectNameProperty =
                RequireProperty(
                    unityObjectType,
                    "name");

            MethodInfo getInstanceIdMethod =
                RequireMethod(
                    unityObjectType,
                    "GetInstanceID",
                    false,
                    0,
                    false);

            PropertyInfo activeInHierarchyProperty =
                RequireProperty(
                    gameObjectType,
                    "activeInHierarchy");

            PropertyInfo sceneProperty =
                RequireProperty(
                    gameObjectType,
                    "scene");

            PropertyInfo transformProperty =
                RequireProperty(
                    gameObjectType,
                    "transform");

            PropertyInfo transformParentProperty =
                RequireProperty(
                    transformType,
                    "parent");

            PropertyInfo sceneNameProperty =
                RequireProperty(
                    sceneType,
                    "name");

            PropertyInfo sceneIsLoadedProperty =
                RequireProperty(
                    sceneType,
                    "isLoaded");

            MethodInfo sceneIsValidMethod =
                RequireMethod(
                    sceneType,
                    "IsValid",
                    false,
                    0,
                    false);

            MethodInfo findObjectsOfTypeAllDefinition =
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

            MethodInfo findObjectsOfTypeAllMethod =
                findObjectsOfTypeAllDefinition
                    .MakeGenericMethod(
                        gameObjectType);

            MethodInfo getComponentsMethod =
                getComponentsDefinition
                    .MakeGenericMethod(
                        componentType);

            return
                new UnityReflection(
                    gameObjectType,
                    componentType,
                    objectNameProperty,
                    getInstanceIdMethod,
                    activeInHierarchyProperty,
                    sceneProperty,
                    transformProperty,
                    transformParentProperty,
                    sceneNameProperty,
                    sceneIsLoadedProperty,
                    sceneIsValidMethod,
                    findObjectsOfTypeAllMethod,
                    getComponentsMethod);
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
                    result)
            )
            {
                if (
                    value is not null &&
                    _gameObjectType.IsInstanceOfType(
                        value)
                )
                {
                    yield return value;
                }
            }
        }

        public bool TryCreateInfo(
            object gameObject,
            out DCMLGameObjectInfo info)
        {
            info =
                null;

            object scene =
                _sceneProperty.GetValue(
                    gameObject);

            if (scene is null)
            {
                return false;
            }

            bool sceneIsValid =
                Convert.ToBoolean(
                    _sceneIsValidMethod.Invoke(
                        scene,
                        null));

            bool sceneIsLoaded =
                Convert.ToBoolean(
                    _sceneIsLoadedProperty.GetValue(
                        scene));

            if (
                !sceneIsValid ||
                !sceneIsLoaded
            )
            {
                return false;
            }

            string name =
                Convert.ToString(
                    _objectNameProperty.GetValue(
                        gameObject))
                ?? string.Empty;

            string sceneName =
                Convert.ToString(
                    _sceneNameProperty.GetValue(
                        scene))
                ?? string.Empty;

            bool activeInHierarchy =
                Convert.ToBoolean(
                    _activeInHierarchyProperty.GetValue(
                        gameObject));

            int instanceId =
                Convert.ToInt32(
                    _getInstanceIdMethod.Invoke(
                        gameObject,
                        null));

            object transform =
                _transformProperty.GetValue(
                    gameObject);

            string hierarchyPath =
                CreateHierarchyPath(
                    transform,
                    name);

            IReadOnlyList<string> componentTypeNames =
                GetComponentTypeNames(
                    gameObject);

            info =
                new DCMLGameObjectInfo(
                    instanceId,
                    name,
                    sceneName,
                    hierarchyPath,
                    activeInHierarchy,
                    componentTypeNames);

            return true;
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
                depth++
            )
            {
                string currentName =
                    Convert.ToString(
                        _objectNameProperty.GetValue(
                            current))
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
                        current)
                )
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

        private IReadOnlyList<string> GetComponentTypeNames(
            object gameObject)
        {
            object components =
                _getComponentsMethod.Invoke(
                    gameObject,
                    null);

            var names =
                new List<string>();

            foreach (
                object component in
                Enumerate(
                    components)
            )
            {
                if (
                    component is null ||
                    !_componentType.IsInstanceOfType(
                        component)
                )
                {
                    continue;
                }

                string typeName =
                    GetComponentTypeName(
                        component);

                if (
                    !string.IsNullOrWhiteSpace(
                        typeName)
                )
                {
                    names.Add(
                        typeName);
                }
            }

            return
                names
                    .Distinct(
                        StringComparer.Ordinal)
                    .OrderBy(
                        value =>
                            value,
                        StringComparer.Ordinal)
                    .ToArray();
        }

        private static string GetComponentTypeName(
            object component)
        {
            string nativeTypeName =
                NativeIl2CppTypeNameResolver.TryGetTypeName(
                    component);

            if (
                !string.IsNullOrWhiteSpace(
                    nativeTypeName)
            )
            {
                return nativeTypeName;
            }

            Type runtimeType =
                component.GetType();

            return
                runtimeType.FullName ??
                runtimeType.Name;
        }

        private static class NativeIl2CppTypeNameResolver
        {
            private static readonly Lazy<Resolver>
                ResolverInstance =
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

                    if (resolver is null)
                    {
                        return string.Empty;
                    }

                    return
                        resolver.GetTypeName(
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
                        getClassNamespaceMethod is null
                    )
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
                private readonly PropertyInfo
                    _objectClassProperty;

                private readonly MethodInfo
                    _getClassNameMethod;

                private readonly MethodInfo
                    _getClassNamespaceMethod;

                public Resolver(
                    PropertyInfo objectClassProperty,
                    MethodInfo getClassNameMethod,
                    MethodInfo getClassNamespaceMethod)
                {
                    _objectClassProperty =
                        objectClassProperty;

                    _getClassNameMethod =
                        getClassNameMethod;

                    _getClassNamespaceMethod =
                        getClassNamespaceMethod;
                }

                public string GetTypeName(
                    object component)
                {
                    object classValue =
                        _objectClassProperty.GetValue(
                            component);

                    if (
                        classValue is not IntPtr
                    )
                    {
                        return string.Empty;
                    }

                    IntPtr classPointer =
                        (IntPtr) classValue;

                    if (
                        classPointer ==
                        IntPtr.Zero
                    )
                    {
                        return string.Empty;
                    }

                    string className =
                        Convert.ToString(
                            _getClassNameMethod.Invoke(
                                null,
                                new object[]
                                {
                                    classPointer
                                }))
                        ?? string.Empty;

                    string classNamespace =
                        Convert.ToString(
                            _getClassNamespaceMethod.Invoke(
                                null,
                                new object[]
                                {
                                    classPointer
                                }))
                        ?? string.Empty;

                    if (
                        string.IsNullOrWhiteSpace(
                            className)
                    )
                    {
                        return string.Empty;
                    }

                    className =
                        className.Trim();

                    classNamespace =
                        classNamespace.Trim();

                    if (
                        classNamespace.StartsWith(
                            "UnityEngine",
                            StringComparison.Ordinal)
                    )
                    {
                        return
                            classNamespace +
                            "." +
                            className;
                    }

                    if (
                        string.Equals(
                            classNamespace,
                            "TMPro",
                            StringComparison.Ordinal)
                    )
                    {
                        return
                            classNamespace +
                            "." +
                            className;
                    }

                    if (
                        classNamespace.Length == 0
                    )
                    {
                        return
                            "Il2Cpp." +
                            className;
                    }

                    return
                        "Il2Cpp." +
                        classNamespace +
                        "." +
                        className;
                }
            }
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

            PropertyInfo lengthProperty =
                value.GetType().GetProperty(
                    "Length",
                    BindingFlags.Public |
                    BindingFlags.Instance);

            PropertyInfo itemProperty =
                value.GetType().GetProperty(
                    "Item",
                    BindingFlags.Public |
                    BindingFlags.Instance);

            if (
                lengthProperty is null ||
                itemProperty is null
            )
            {
                throw new InvalidOperationException(
                    $"Unity collection type '{value.GetType().FullName}' is not enumerable.");
            }

            int length =
                Convert.ToInt32(
                    lengthProperty.GetValue(
                        value));

            for (
                int index = 0;
                index < length;
                index++
            )
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
                AppDomain.CurrentDomain.GetAssemblies()
            )
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
                $"DCML could not locate loaded Unity type '{fullName}'.");
        }

        private static PropertyInfo RequireProperty(
            Type type,
            string name)
        {
            PropertyInfo property =
                type.GetProperty(
                    name,
                    BindingFlags.Public |
                    BindingFlags.Instance |
                    BindingFlags.Static);

            return
                property ??
                throw new InvalidOperationException(
                    $"DCML could not locate Unity property '{type.FullName}.{name}'.");
        }

        private static MethodInfo RequireMethod(
            Type type,
            string name,
            bool isStatic,
            int parameterCount,
            bool isGeneric)
        {
            MethodInfo method =
                type
                    .GetMethods(
                        BindingFlags.Public |
                        BindingFlags.Instance |
                        BindingFlags.Static)
                    .FirstOrDefault(
                        candidate =>
                            candidate.Name == name &&
                            candidate.IsStatic == isStatic &&
                            candidate.GetParameters().Length ==
                                parameterCount &&
                            candidate.IsGenericMethodDefinition ==
                                isGeneric &&
                            (
                                !isGeneric ||
                                candidate.GetGenericArguments().Length ==
                                    1
                            ));

            return
                method ??
                throw new InvalidOperationException(
                    $"DCML could not locate required Unity method '{type.FullName}.{name}'.");
        }
    }
}
