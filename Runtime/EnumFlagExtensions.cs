using System;
using System.Runtime.CompilerServices;
#if !CSHARP_7_3_OR_NEWER
using Unity.Collections.LowLevel.Unsafe;
#endif

namespace Megumin
{
    /// <summary>
    /// https://forum.unity.com/threads/c-hasaflag-method-extension-how-to-not-create-garbage-allocation.616924/#post-7271224
    /// </summary>
    public static class EnumFlagExtensions_EC8A1CFA9AF14EBD81DDA8DE4231C0F4
    {
#if CSHARP_7_3_OR_NEWER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [MethodImpl((MethodImplOptions)256)]
#endif
        public static TEnum AddFlag<TEnum>(this TEnum lhs, TEnum rhs) where TEnum :
#if CSHARP_7_3_OR_NEWER
                    unmanaged, Enum
#else
                    struct
#endif
        {

            unsafe
            {
#if CSHARP_7_3_OR_NEWER
                switch (sizeof(TEnum))
                {
                    case 1:
                        {
                            var r = *(byte*)(&lhs) | *(byte*)(&rhs);
                            return *(TEnum*)&r;
                        }
                    case 2:
                        {
                            var r = *(ushort*)(&lhs) | *(ushort*)(&rhs);
                            return *(TEnum*)&r;
                        }
                    case 4:
                        {
                            var r = *(uint*)(&lhs) | *(uint*)(&rhs);
                            return *(TEnum*)&r;
                        }
                    case 8:
                        {
                            var r = *(ulong*)(&lhs) | *(ulong*)(&rhs);
                            return *(TEnum*)&r;
                        }
                    default:
                        throw new Exception("Size does not match a known Enum backing type.");
                }

#else
                 
            switch (UnsafeUtility.SizeOf<TEnum>())
            {
                case 1:
                    {
                        byte valLhs = 0;
                        UnsafeUtility.CopyStructureToPtr(ref lhs, &valLhs);
                        byte valRhs = 0;
                        UnsafeUtility.CopyStructureToPtr(ref rhs, &valRhs);
                        var result = (valLhs | valRhs);
                        void * r = &result;
                        TEnum o;
                        UnsafeUtility.CopyPtrToStructure(r, out o);
                        return o;
                    }
                case 2:
                    {
                        ushort valLhs = 0;
                        UnsafeUtility.CopyStructureToPtr(ref lhs, &valLhs);
                        ushort valRhs = 0;
                        UnsafeUtility.CopyStructureToPtr(ref rhs, &valRhs);
                        var result = (valLhs | valRhs);
                        void* r = &result;
                        TEnum o;
                        UnsafeUtility.CopyPtrToStructure(r, out o);
                        return o;
                    }
                case 4:
                    {
                        uint valLhs = 0;
                        UnsafeUtility.CopyStructureToPtr(ref lhs, &valLhs);
                        uint valRhs = 0;
                        UnsafeUtility.CopyStructureToPtr(ref rhs, &valRhs);
                        var result = (valLhs | valRhs);
                        void* r = &result;
                        TEnum o;
                        UnsafeUtility.CopyPtrToStructure(r, out o);
                        return o;
                    }
                case 8:
                    {
                        ulong valLhs = 0;
                        UnsafeUtility.CopyStructureToPtr(ref lhs, &valLhs);
                        ulong valRhs = 0;
                        UnsafeUtility.CopyStructureToPtr(ref rhs, &valRhs);
                        var result = (valLhs | valRhs);
                        void* r = &result;
                        TEnum o;
                        UnsafeUtility.CopyPtrToStructure(r, out o);
                        return o;
                    }
                default:
                    throw new Exception("Size does not match a known Enum backing type.");
            }
#endif
            }
        }


#if CSHARP_7_3_OR_NEWER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [MethodImpl((MethodImplOptions)256)]
#endif
        public static TEnum RemoveFlag<TEnum>(this TEnum lhs, TEnum rhs) where TEnum :
#if CSHARP_7_3_OR_NEWER
                    unmanaged, Enum
#else
                    struct
#endif
        {

            unsafe
            {
#if CSHARP_7_3_OR_NEWER
                switch (sizeof(TEnum))
                {
                    case 1:
                        {
                            var r = *(byte*)(&lhs) & ~*(byte*)(&rhs);
                            return *(TEnum*)&r;
                        }
                    case 2:
                        {
                            var r = *(ushort*)(&lhs) & ~*(ushort*)(&rhs);
                            return *(TEnum*)&r;
                        }
                    case 4:
                        {
                            var r = *(uint*)(&lhs) & ~*(uint*)(&rhs);
                            return *(TEnum*)&r;
                        }
                    case 8:
                        {
                            var r = *(ulong*)(&lhs) & ~*(ulong*)(&rhs);
                            return *(TEnum*)&r;
                        }
                    default:
                        throw new Exception("Size does not match a known Enum backing type.");
                }

#else
                 
            switch (UnsafeUtility.SizeOf<TEnum>())
            {
                case 1:
                    {
                        byte valLhs = 0;
                        UnsafeUtility.CopyStructureToPtr(ref lhs, &valLhs);
                        byte valRhs = 0;
                        UnsafeUtility.CopyStructureToPtr(ref rhs, &valRhs);
                        var result = (valLhs & ~valRhs);
                        void * r = &result;
                        TEnum o;
                        UnsafeUtility.CopyPtrToStructure(r, out o);
                        return o;
                    }
                case 2:
                    {
                        ushort valLhs = 0;
                        UnsafeUtility.CopyStructureToPtr(ref lhs, &valLhs);
                        ushort valRhs = 0;
                        UnsafeUtility.CopyStructureToPtr(ref rhs, &valRhs);
                        var result = (valLhs & ~valRhs);
                        void* r = &result;
                        TEnum o;
                        UnsafeUtility.CopyPtrToStructure(r, out o);
                        return o;
                    }
                case 4:
                    {
                        uint valLhs = 0;
                        UnsafeUtility.CopyStructureToPtr(ref lhs, &valLhs);
                        uint valRhs = 0;
                        UnsafeUtility.CopyStructureToPtr(ref rhs, &valRhs);
                        var result = (valLhs & ~valRhs);
                        void* r = &result;
                        TEnum o;
                        UnsafeUtility.CopyPtrToStructure(r, out o);
                        return o;
                    }
                case 8:
                    {
                        ulong valLhs = 0;
                        UnsafeUtility.CopyStructureToPtr(ref lhs, &valLhs);
                        ulong valRhs = 0;
                        UnsafeUtility.CopyStructureToPtr(ref rhs, &valRhs);
                        var result = (valLhs & ~valRhs);
                        void* r = &result;
                        TEnum o;
                        UnsafeUtility.CopyPtrToStructure(r, out o);
                        return o;
                    }
                default:
                    throw new Exception("Size does not match a known Enum backing type.");
            }
#endif
            }

        }

#if CSHARP_7_3_OR_NEWER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetFlag<TEnum>(ref this TEnum lhs, TEnum rhs) where TEnum : unmanaged, Enum
        {

            unsafe
            {
                fixed (TEnum* lhs1 = &lhs)
                {
                    switch (sizeof(TEnum))
                    {
                        case 1:
                            {
                                var r = *(byte*)(lhs1) | *(byte*)(&rhs);
                                *lhs1 = *(TEnum*)&r;
                                return;
                            }
                        case 2:
                            {
                                var r = *(ushort*)(lhs1) | *(ushort*)(&rhs);
                                *lhs1 = *(TEnum*)&r;
                                return;
                            }
                        case 4:
                            {
                                var r = *(uint*)(lhs1) | *(uint*)(&rhs);
                                *lhs1 = *(TEnum*)&r;
                                return;
                            }
                        case 8:
                            {
                                var r = *(ulong*)(lhs1) | *(ulong*)(&rhs);
                                *lhs1 = *(TEnum*)&r;
                                return;
                            }
                        default:
                            throw new Exception("Size does not match a known Enum backing type.");
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClearFlag<TEnum>(this ref TEnum lhs, TEnum rhs) where TEnum : unmanaged, Enum
        {

            unsafe
            {
                fixed (TEnum* lhs1 = &lhs)
                {
                    switch (sizeof(TEnum))
                    {
                        case 1:
                            {
                                var r = *(byte*)(lhs1) & ~*(byte*)(&rhs);
                                *lhs1 = *(TEnum*)&r;
                                return;
                            }
                        case 2:
                            {
                                var r = *(ushort*)(lhs1) & ~*(ushort*)(&rhs);
                                *lhs1 = *(TEnum*)&r;
                                return;
                            }
                        case 4:
                            {
                                var r = *(uint*)(lhs1) & ~*(uint*)(&rhs);
                                *lhs1 = *(TEnum*)&r;
                                return;
                            }
                        case 8:
                            {
                                var r = *(ulong*)(lhs1) & ~*(ulong*)(&rhs);
                                *lhs1 = *(TEnum*)&r;
                                return;
                            }
                        default:
                            throw new Exception("Size does not match a known Enum backing type.");
                    }
                }
            }
        }
#endif
    }


}



