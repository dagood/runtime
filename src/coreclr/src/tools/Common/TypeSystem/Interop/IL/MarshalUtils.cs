// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Internal.TypeSystem.Interop
{
    public static class MarshalUtils
    {
        /// <summary>
        /// Returns true if this type has a common representation in both managed and unmanaged memory
        /// and does not require special handling by the interop marshaler.
        /// </summary>
        public static bool IsBlittableType(TypeDesc type)
        {
            if (!type.IsDefType)
            {
                return false;
            }

            TypeDesc baseType = type.BaseType;
            bool hasNonTrivialParent = baseType != null
                && !baseType.IsWellKnownType(WellKnownType.Object)
                && !baseType.IsWellKnownType(WellKnownType.ValueType);

            if (hasNonTrivialParent && !IsBlittableType(baseType))
            {
                return false;
            }

            var mdType = (MetadataType)type;

            if (!mdType.IsSequentialLayout && !mdType.IsExplicitLayout)
            {
                return false;
            }

            foreach (FieldDesc field in type.GetFields())
            {
                if (field.IsStatic)
                {
                    continue;
                }

                if (!field.FieldType.IsValueType)
                {
                    // Types with fields of non-value types cannot be blittable
                    // This check prevents possible infinite recursion where GetMarshallerKind would call back to IsBlittable e.g. for
                    // the case of classes with pointer members to the class itself.
                    return false;
                }

                MarshallerKind marshallerKind = MarshalHelpers.GetMarshallerKind(
                    field.FieldType,
                    field.GetMarshalAsDescriptor(),
                    isReturn: false,
                    isAnsi: mdType.PInvokeStringFormat == PInvokeStringFormat.AnsiClass,
                    MarshallerType.Field,
                    elementMarshallerKind: out var _);

                if (marshallerKind != MarshallerKind.Enum
                    && marshallerKind != MarshallerKind.BlittableValue
                    && marshallerKind != MarshallerKind.BlittableStruct
                    && marshallerKind != MarshallerKind.UnicodeChar)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
