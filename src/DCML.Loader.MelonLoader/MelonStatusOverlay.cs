using System;
using System.Collections.Generic;
using DCML.Core.Models;
using DCML.Core.Runtime;
using UnityEngine;

namespace DCML.Loader.MelonLoader
{
    /// <summary>
    /// Optional, read-only Unity IMGUI status overlay for the
    /// MelonLoader host.
    /// </summary>
    internal sealed class MelonStatusOverlay
    {
        private const string StatusUiEnvironmentVariable =
            "DCML_STATUS_UI";

        private readonly Action<string> _info;
        private readonly Action<string> _warning;

        private bool _visible;
        private bool _inputDisabled;
        private bool _inputWarningLogged;
        private bool _guiDisabled;
        private bool _guiWarningLogged;
        private bool _renderReadyLogged;

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
            if (_inputDisabled)
            {
                return;
            }

            try
            {
                if (
                    !Input.GetKeyDown(
                        KeyCode.F8
                    )
                )
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
            catch (Exception exception)
            {
                DisableInput(
                    exception
                );
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

                GUI.Box(
                    new Rect(
                        20.0f,
                        20.0f,
                        760.0f,
                        panelHeight
                    ),
                    "DCML Module Status - F8 to hide"
                );

                float y =
                    50.0f;

                foreach (
                    string line
                    in lines
                )
                {
                    GUI.Label(
                        new Rect(
                            34.0f,
                            y,
                            732.0f,
                            22.0f
                        ),
                        line
                    );

                    y +=
                        22.0f;
                }

                if (!_renderReadyLogged)
                {
                    _renderReadyLogged =
                        true;

                    _info(
                        "Status UI render ready."
                    );
                }
            }
            catch (Exception exception)
            {
                DisableGui(
                    exception
                );
            }
        }

        private void DisableInput(
            Exception exception
        )
        {
            _inputDisabled =
                true;

            if (_inputWarningLogged)
            {
                return;
            }

            _inputWarningLogged =
                true;

            _warning(
                "Status UI input failed and has been disabled." +
                FormatExceptionSuffix(
                    exception
                )
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

            _warning(
                "Status UI rendering failed and has been disabled." +
                FormatExceptionSuffix(
                    exception
                )
            );
        }

        private static string FormatExceptionSuffix(
            Exception exception
        )
        {
            if (exception == null)
            {
                return string.Empty;
            }

            return
                " " +
                exception.GetType().Name +
                ": " +
                exception.Message;
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
