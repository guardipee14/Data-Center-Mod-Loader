using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DCML.Core.Models;
using DCML.Core.Runtime;

namespace DCML.Loader.MelonLoader
{
    /// <summary>
    /// Optional, read-only Unity IMGUI status overlay for the
    /// MelonLoader host. Unity types are resolved dynamically so
    /// failure to initialize the overlay cannot prevent DCML startup.
    /// </summary>
    internal sealed class MelonStatusOverlay
    {
        private const string StatusUiEnvironmentVariable =
            "DCML_STATUS_UI";

        private readonly Action<string> _info;
        private readonly Action<string> _warning;

        private bool _visible;
        private bool _guiDisabled;
        private bool _guiWarningLogged;

        private Type _rectType;
        private ConstructorInfo _rectConstructor;
        private MethodInfo _guiBox;
        private MethodInfo _guiLabel;

        private Type _keyCodeType;
        private MethodInfo _inputGetKeyDown;
        private object _toggleKey;

        public MelonStatusOverlay(
            Action<string> info,
            Action<string> warning
        )
        {
            _info =
                info ??
                throw new ArgumentNullException(
                    nameof(info)
                );

            _warning =
                warning ??
                throw new ArgumentNullException(
                    nameof(warning)
                );

            _visible =
                ReadInitialVisibility();
        }

        public bool Visible =>
            _visible;

        public void UpdateToggle()
        {
            if (
                !TryInitializeInput()
            )
            {
                return;
            }

            try
            {
                bool pressed =
                    (bool) _inputGetKeyDown.Invoke(
                        null,
                        new[]
                        {
                            _toggleKey
                        }
                    );

                if (!pressed)
                {
                    return;
                }

                _visible =
                    !_visible;

                _info(
                    "Status UI " +
                    (
                        _visible
                            ? "shown"
                            : "hidden"
                    ) +
                    "."
                );
            }
            catch
            {
                _inputGetKeyDown =
                    null;

                _keyCodeType =
                    null;

                _toggleKey =
                    null;
            }
        }

        public void Draw(
            DCMLDiagnosticsSnapshot snapshot
        )
        {
            if (
                !_visible ||
                snapshot == null ||
                _guiDisabled
            )
            {
                return;
            }

            if (
                !TryInitializeGui()
            )
            {
                return;
            }

            try
            {
                IReadOnlyList<string> lines =
                    DCMLStatusPanelText.Build(
                        snapshot,
                        maxModules:
                            8,
                        maxDiagnostics:
                            6
                    );

                float panelHeight =
                    54.0f +
                    (
                        lines.Count *
                        22.0f
                    );

                object panelRect =
                    CreateRect(
                        20.0f,
                        20.0f,
                        760.0f,
                        panelHeight
                    );

                _guiBox.Invoke(
                    null,
                    new[]
                    {
                        panelRect,
                        "DCML Module Status - F8 to hide"
                    }
                );

                float y =
                    50.0f;

                foreach (
                    string line
                    in lines
                )
                {
                    object lineRect =
                        CreateRect(
                            34.0f,
                            y,
                            732.0f,
                            22.0f
                        );

                    _guiLabel.Invoke(
                        null,
                        new[]
                        {
                            lineRect,
                            line
                        }
                    );

                    y +=
                        22.0f;
                }
            }
            catch (Exception exception)
            {
                DisableGui(
                    exception
                );
            }
        }

        private bool TryInitializeGui()
        {
            if (_guiDisabled)
            {
                return false;
            }

            if (
                _rectConstructor != null &&
                _guiBox != null &&
                _guiLabel != null
            )
            {
                return true;
            }

            _rectType =
                FindLoadedType(
                    "UnityEngine.Rect"
                );

            Type guiType =
                FindLoadedType(
                    "UnityEngine.GUI"
                );

            if (
                _rectType == null ||
                guiType == null
            )
            {
                return false;
            }

            _rectConstructor =
                _rectType.GetConstructor(
                    new[]
                    {
                        typeof(float),
                        typeof(float),
                        typeof(float),
                        typeof(float)
                    }
                );

            _guiBox =
                guiType.GetMethod(
                    "Box",
                    BindingFlags.Public |
                    BindingFlags.Static,
                    null,
                    new[]
                    {
                        _rectType,
                        typeof(string)
                    },
                    null
                );

            _guiLabel =
                guiType.GetMethod(
                    "Label",
                    BindingFlags.Public |
                    BindingFlags.Static,
                    null,
                    new[]
                    {
                        _rectType,
                        typeof(string)
                    },
                    null
                );

            if (
                _rectConstructor == null ||
                _guiBox == null ||
                _guiLabel == null
            )
            {
                DisableGui(
                    null
                );

                return false;
            }

            return true;
        }

        private bool TryInitializeInput()
        {
            if (
                _inputGetKeyDown != null &&
                _toggleKey != null
            )
            {
                return true;
            }

            Type inputType =
                FindLoadedType(
                    "UnityEngine.Input"
                );

            _keyCodeType =
                FindLoadedType(
                    "UnityEngine.KeyCode"
                );

            if (
                inputType == null ||
                _keyCodeType == null
            )
            {
                return false;
            }

            _inputGetKeyDown =
                inputType.GetMethod(
                    "GetKeyDown",
                    BindingFlags.Public |
                    BindingFlags.Static,
                    null,
                    new[]
                    {
                        _keyCodeType
                    },
                    null
                );

            if (_inputGetKeyDown == null)
            {
                return false;
            }

            try
            {
                _toggleKey =
                    Enum.Parse(
                        _keyCodeType,
                        "F8"
                    );
            }
            catch
            {
                _inputGetKeyDown =
                    null;

                _keyCodeType =
                    null;

                _toggleKey =
                    null;

                return false;
            }

            return true;
        }

        private object CreateRect(
            float x,
            float y,
            float width,
            float height
        )
        {
            return _rectConstructor.Invoke(
                new object[]
                {
                    x,
                    y,
                    width,
                    height
                }
            );
        }

        private void DisableGui(
            Exception exception
        )
        {
            _guiDisabled =
                true;

            if (_guiWarningLogged)
            {
                return;
            }

            _guiWarningLogged =
                true;

            string suffix =
                exception == null
                    ? string.Empty
                    : " " +
                      exception.GetType().Name +
                      ": " +
                      exception.Message;

            _warning(
                "Status UI could not initialize and has been disabled." +
                suffix
            );
        }

        private static Type FindLoadedType(
            string fullName
        )
        {
            foreach (
                Assembly assembly
                in AppDomain.CurrentDomain
                    .GetAssemblies()
            )
            {
                Type type;

                try
                {
                    type =
                        assembly.GetType(
                            fullName,
                            false
                        );
                }
                catch
                {
                    continue;
                }

                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static bool ReadInitialVisibility()
        {
            string value =
                Environment.GetEnvironmentVariable(
                    StatusUiEnvironmentVariable
                );

            if (
                string.IsNullOrWhiteSpace(
                    value
                )
            )
            {
                return false;
            }

            return
                string.Equals(
                    value,
                    "1",
                    StringComparison.OrdinalIgnoreCase
                ) ||
                string.Equals(
                    value,
                    "true",
                    StringComparison.OrdinalIgnoreCase
                ) ||
                string.Equals(
                    value,
                    "yes",
                    StringComparison.OrdinalIgnoreCase
                ) ||
                string.Equals(
                    value,
                    "on",
                    StringComparison.OrdinalIgnoreCase
                );
        }
    }
}
