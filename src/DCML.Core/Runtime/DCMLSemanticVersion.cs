using System;

namespace DCML.Core.Runtime
{
    /// <summary>
    /// Provides lightweight Semantic Versioning 2.0.0 validation
    /// and precedence comparison.
    /// </summary>
    public static class DCMLSemanticVersion
    {
        /// <summary>
        /// Determines whether a value is a valid Semantic Versioning
        /// 2.0.0 version string.
        /// </summary>
        public static bool IsValid(
            string? value
        )
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string version = value;

            int buildIndex =
                version.IndexOf('+');

            if (buildIndex >= 0)
            {
                if (
                    version.IndexOf(
                        '+',
                        buildIndex + 1
                    ) >= 0
                )
                {
                    return false;
                }

                string buildMetadata =
                    version.Substring(
                        buildIndex + 1
                    );

                if (
                    !ValidateIdentifierList(
                        buildMetadata,
                        false
                    )
                )
                {
                    return false;
                }

                version =
                    version.Substring(
                        0,
                        buildIndex
                    );
            }

            int prereleaseIndex =
                version.IndexOf('-');

            if (prereleaseIndex >= 0)
            {
                string prerelease =
                    version.Substring(
                        prereleaseIndex + 1
                    );

                if (
                    !ValidateIdentifierList(
                        prerelease,
                        true
                    )
                )
                {
                    return false;
                }

                version =
                    version.Substring(
                        0,
                        prereleaseIndex
                    );
            }

            string[] coreParts =
                version.Split('.');

            if (coreParts.Length != 3)
            {
                return false;
            }

            foreach (string part in coreParts)
            {
                if (!ValidateCoreNumber(part))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Compares two valid semantic versions according to SemVer
        /// precedence rules. Build metadata does not affect precedence.
        /// </summary>
        public static bool TryCompare(
            string? left,
            string? right,
            out int comparison
        )
        {
            comparison = 0;

            if (
                !IsValid(left) ||
                !IsValid(right)
            )
            {
                return false;
            }

            string leftValue =
                left!;

            string rightValue =
                right!;

            SplitVersion(
                leftValue,
                out string[] leftCore,
                out string[]? leftPrerelease
            );

            SplitVersion(
                rightValue,
                out string[] rightCore,
                out string[]? rightPrerelease
            );

            for (
                int index = 0;
                index < 3;
                index++
            )
            {
                comparison =
                    CompareNumericIdentifier(
                        leftCore[index],
                        rightCore[index]
                    );

                if (comparison != 0)
                {
                    return true;
                }
            }

            if (
                leftPrerelease == null &&
                rightPrerelease == null
            )
            {
                comparison = 0;
                return true;
            }

            if (leftPrerelease == null)
            {
                comparison = 1;
                return true;
            }

            if (rightPrerelease == null)
            {
                comparison = -1;
                return true;
            }

            int commonLength =
                Math.Min(
                    leftPrerelease.Length,
                    rightPrerelease.Length
                );

            for (
                int index = 0;
                index < commonLength;
                index++
            )
            {
                string leftIdentifier =
                    leftPrerelease[index];

                string rightIdentifier =
                    rightPrerelease[index];

                bool leftNumeric =
                    IsNumericIdentifier(
                        leftIdentifier
                    );

                bool rightNumeric =
                    IsNumericIdentifier(
                        rightIdentifier
                    );

                if (
                    leftNumeric &&
                    rightNumeric
                )
                {
                    comparison =
                        CompareNumericIdentifier(
                            leftIdentifier,
                            rightIdentifier
                        );
                }
                else if (
                    leftNumeric &&
                    !rightNumeric
                )
                {
                    comparison = -1;
                }
                else if (
                    !leftNumeric &&
                    rightNumeric
                )
                {
                    comparison = 1;
                }
                else
                {
                    comparison =
                        string.CompareOrdinal(
                            leftIdentifier,
                            rightIdentifier
                        );
                }

                if (comparison != 0)
                {
                    comparison =
                        comparison < 0
                            ? -1
                            : 1;

                    return true;
                }
            }

            comparison =
                leftPrerelease.Length.CompareTo(
                    rightPrerelease.Length
                );

            if (comparison != 0)
            {
                comparison =
                    comparison < 0
                        ? -1
                        : 1;
            }

            return true;
        }

        private static void SplitVersion(
            string value,
            out string[] core,
            out string[]? prerelease
        )
        {
            int buildIndex =
                value.IndexOf('+');

            string withoutBuild =
                buildIndex >= 0
                    ? value.Substring(
                        0,
                        buildIndex
                    )
                    : value;

            int prereleaseIndex =
                withoutBuild.IndexOf('-');

            string coreText;

            if (prereleaseIndex >= 0)
            {
                coreText =
                    withoutBuild.Substring(
                        0,
                        prereleaseIndex
                    );

                prerelease =
                    withoutBuild.Substring(
                        prereleaseIndex + 1
                    ).Split('.');
            }
            else
            {
                coreText =
                    withoutBuild;

                prerelease =
                    null;
            }

            core =
                coreText.Split('.');
        }

        private static int CompareNumericIdentifier(
            string left,
            string right
        )
        {
            if (left.Length != right.Length)
            {
                return left.Length < right.Length
                    ? -1
                    : 1;
            }

            int comparison =
                string.CompareOrdinal(
                    left,
                    right
                );

            if (comparison == 0)
            {
                return 0;
            }

            return comparison < 0
                ? -1
                : 1;
        }

        private static bool IsNumericIdentifier(
            string value
        )
        {
            foreach (char character in value)
            {
                if (
                    character < '0' ||
                    character > '9'
                )
                {
                    return false;
                }
            }

            return value.Length > 0;
        }

        private static bool ValidateCoreNumber(
            string value
        )
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            if (
                value.Length > 1 &&
                value[0] == '0'
            )
            {
                return false;
            }

            foreach (char character in value)
            {
                if (
                    character < '0' ||
                    character > '9'
                )
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateIdentifierList(
            string value,
            bool enforceNumericLeadingZeroRule
        )
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            string[] identifiers =
                value.Split('.');

            foreach (string identifier in identifiers)
            {
                if (string.IsNullOrEmpty(identifier))
                {
                    return false;
                }

                bool numeric = true;

                foreach (char character in identifier)
                {
                    bool valid =
                        character >= '0' &&
                        character <= '9' ||
                        character >= 'A' &&
                        character <= 'Z' ||
                        character >= 'a' &&
                        character <= 'z' ||
                        character == '-';

                    if (!valid)
                    {
                        return false;
                    }

                    if (
                        character < '0' ||
                        character > '9'
                    )
                    {
                        numeric = false;
                    }
                }

                if (
                    enforceNumericLeadingZeroRule &&
                    numeric &&
                    identifier.Length > 1 &&
                    identifier[0] == '0'
                )
                {
                    return false;
                }
            }

            return true;
        }
    }
}
