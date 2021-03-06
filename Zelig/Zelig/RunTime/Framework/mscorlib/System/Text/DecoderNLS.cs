// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==
namespace System.Text
{
    using System;
    using System.Text;
////using System.Runtime.Serialization;
////using System.Security.Permissions;
    // A Decoder is used to decode a sequence of blocks of bytes into a
    // sequence of blocks of characters. Following instantiation of a decoder,
    // sequential blocks of bytes are converted into blocks of characters through
    // calls to the GetChars method. The decoder maintains state between the
    // conversions, allowing it to correctly decode byte sequences that span
    // adjacent blocks.
    //
    // Instances of specific implementations of the Decoder abstract base
    // class are typically obtained through calls to the GetDecoder method
    // of Encoding objects.
    //

    [Serializable()] 
    internal class DecoderNLS : Decoder /*, ISerializable*/
    {
        // Remember our encoding
                        protected   Encoding m_encoding;
        [NonSerialized] protected   bool     m_mustFlush;
        [NonSerialized] internal    bool     m_throwOnOverflow;
        [NonSerialized] internal    int      m_bytesUsed;

#region Serialization

////    // Constructor called by serialization. called during deserialization.
////    internal DecoderNLS(SerializationInfo info, StreamingContext context)
////    {
////        throw new NotSupportedException(
////                    String.Format(
////                        System.Globalization.CultureInfo.CurrentCulture, 
////                        Environment.GetResourceString("NotSupported_TypeCannotDeserialized"), this.GetType()));
////    }
////
////    // ISerializable implementation. called during serialization.
////    [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.SerializationFormatter)]
////    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
////    {
////        SerializeDecoder(info);
////        info.AddValue("encoding", this.m_encoding);
////        info.SetType(typeof(Encoding.DefaultDecoder));
////    }

#endregion Serialization 

        internal DecoderNLS( Encoding encoding )
        {
            this.m_encoding = encoding;
            this.m_fallback = this.m_encoding.DecoderFallback;
            this.Reset();
        }

        // This is used by our child deserializers
        internal DecoderNLS( )
        {
            this.m_encoding = null;
            this.Reset();
        }

        public override void Reset()
        {
            if (m_fallbackBuffer != null)
                m_fallbackBuffer.Reset();
        }

        public override unsafe int GetCharCount(byte[] bytes, int index, int count)
        {
            return GetCharCount(bytes, index, count, false);
        }

        public override unsafe int GetCharCount(byte[] bytes, int index, int count, bool flush)
        {
            // Validate Parameters
            if (bytes == null)
            {
#if EXCEPTION_STRINGS
                throw new ArgumentNullException("bytes", Environment.GetResourceString("ArgumentNull_Array"));
#else
                throw new ArgumentNullException();
#endif
            }

            if (index < 0 || count < 0)
            {
#if EXCEPTION_STRINGS
                throw new ArgumentOutOfRangeException((index<0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
#else
                throw new ArgumentOutOfRangeException();
#endif
            }

            if (bytes.Length - index < count)
            {
#if EXCEPTION_STRINGS
                throw new ArgumentOutOfRangeException("bytes", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
#else
                throw new ArgumentOutOfRangeException();
#endif
            }


            // Avoid null fixed problem
            if (bytes.Length == 0)
            {
                bytes = new byte[1];
            }

            // Just call pointer version
            fixed (byte* pBytes = bytes)
            {
                return GetCharCount(pBytes + index, count, flush);
            }
        }

        public unsafe override int GetCharCount(byte* bytes, int count, bool flush)
        {
            // Validate parameters
            if (bytes == null)
            {
#if EXCEPTION_STRINGS
                throw new ArgumentNullException("bytes", Environment.GetResourceString("ArgumentNull_Array"));
#else
                throw new ArgumentNullException();
#endif
            }

            if (count < 0)
            {
#if EXCEPTION_STRINGS
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
#else
                throw new ArgumentOutOfRangeException();
#endif
            }

            // Remember the flush
            this.m_mustFlush = flush;
            this.m_throwOnOverflow = true;

            // By default just call the encoding version, no flush by default
            return m_encoding.GetCharCount(bytes, count, this);
        }

        public override unsafe int GetChars( byte[] bytes     ,
                                             int    byteIndex ,
                                             int    byteCount ,
                                             char[] chars     ,
                                             int    charIndex )
        {
            return GetChars(bytes, byteIndex, byteCount, chars, charIndex, false);
        }

        public override unsafe int GetChars( byte[] bytes     ,
                                             int    byteIndex ,
                                             int    byteCount ,
                                             char[] chars     ,
                                             int    charIndex ,
                                             bool   flush     )
        {
            // Validate Parameters
            if (bytes == null || chars == null)
            {
#if EXCEPTION_STRINGS
                throw new ArgumentNullException(bytes == null ? "bytes" : "chars", Environment.GetResourceString("ArgumentNull_Array"));
#else
                throw new ArgumentNullException();
#endif
            }

            if (byteIndex < 0 || byteCount < 0)
            {
#if EXCEPTION_STRINGS
                throw new ArgumentOutOfRangeException((byteIndex<0 ? "byteIndex" : "byteCount"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
#else
                throw new ArgumentOutOfRangeException();
#endif
            }

            if ( bytes.Length - byteIndex < byteCount)
            {
#if EXCEPTION_STRINGS
                throw new ArgumentOutOfRangeException("bytes", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
#else
                throw new ArgumentOutOfRangeException();
#endif
            }

            if (charIndex < 0 || charIndex > chars.Length)
            {
#if EXCEPTION_STRINGS
                throw new ArgumentOutOfRangeException("charIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
#else
                throw new ArgumentOutOfRangeException();
#endif
            }


            // Avoid empty input fixed problem
            if (bytes.Length == 0)
            {
                bytes = new byte[1];
            }

            int charCount = chars.Length - charIndex;
            if (chars.Length == 0)
            {
                chars = new char[1];
            }

            // Just call pointer version
            fixed (byte* pBytes = bytes)
            {
                fixed (char* pChars = chars)
                {
                    // Remember that charCount is # to decode, not size of array
                    return GetChars(pBytes + byteIndex, byteCount, pChars + charIndex, charCount, flush);
                }
            }
        }

        public unsafe override int GetChars( byte* bytes     ,
                                             int   byteCount ,
                                             char* chars     ,
                                             int   charCount ,
                                             bool  flush     )
        {
            // Validate parameters
            if (chars == null || bytes == null)
            {
#if EXCEPTION_STRINGS
                throw new ArgumentNullException((chars == null ? "chars" : "bytes"), Environment.GetResourceString("ArgumentNull_Array"));
#else
                throw new ArgumentNullException();
#endif
            }

            if (byteCount < 0 || charCount < 0)
            {
#if EXCEPTION_STRINGS
                throw new ArgumentOutOfRangeException((byteCount<0 ? "byteCount" : "charCount"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
#else
                throw new ArgumentOutOfRangeException();
#endif
            }

            // Remember our flush
            m_mustFlush = flush;
            m_throwOnOverflow = true;

            // By default just call the encoding's version
            return m_encoding.GetChars(bytes, byteCount, chars, charCount, this);
        }

        // This method is used when the output buffer might not be big enough.
        // Just call the pointer version.  (This gets chars)
        public override unsafe void Convert(     byte[] bytes     ,
                                                 int    byteIndex ,
                                                 int    byteCount ,
                                                 char[] chars     ,
                                                 int    charIndex ,
                                                 int    charCount ,
                                                 bool   flush     ,
                                             out int    bytesUsed ,
                                             out int    charsUsed ,
                                             out bool   completed )
        {
            // Validate parameters
            if (bytes == null || chars == null)
            {
#if EXCEPTION_STRINGS
                throw new ArgumentNullException((bytes == null ? "bytes" : "chars"), Environment.GetResourceString("ArgumentNull_Array"));
#else
                throw new ArgumentNullException();
#endif
            }

            if (byteIndex < 0 || byteCount < 0)
            {
#if EXCEPTION_STRINGS
                throw new ArgumentOutOfRangeException((byteIndex<0 ? "byteIndex" : "byteCount"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
#else
                throw new ArgumentOutOfRangeException();
#endif
            }

            if (charIndex < 0 || charCount < 0)
            {
#if EXCEPTION_STRINGS
                throw new ArgumentOutOfRangeException((charIndex<0 ? "charIndex" : "charCount"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
#else
                throw new ArgumentOutOfRangeException();
#endif
            }

            if (bytes.Length - byteIndex < byteCount)
            {
#if EXCEPTION_STRINGS
                throw new ArgumentOutOfRangeException("bytes", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
#else
                throw new ArgumentOutOfRangeException();
#endif
            }

            if (chars.Length - charIndex < charCount)
            {
#if EXCEPTION_STRINGS
                throw new ArgumentOutOfRangeException("chars", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
#else
                throw new ArgumentOutOfRangeException();
#endif
            }


            // Avoid empty input problem
            if (bytes.Length == 0)
            {
                bytes = new byte[1];
            }
            if (chars.Length == 0)
            {
                chars = new char[1];
            }

            // Just call the pointer version (public overrides can't do this)
            fixed (byte* pBytes = bytes)
            {
                fixed (char* pChars = chars)
                {
                    Convert(pBytes + byteIndex, byteCount, pChars + charIndex, charCount, flush, out bytesUsed, out charsUsed, out completed);
                }
            }
        }

        // This is the version that used pointers.  We call the base encoding worker function
        // after setting our appropriate internal variables.  This is getting chars
        public unsafe override void Convert(     byte* bytes     ,
                                                 int   byteCount ,
                                                 char* chars     ,
                                                 int   charCount ,
                                                 bool  flush     ,
                                             out int   bytesUsed ,
                                             out int   charsUsed ,
                                             out bool  completed )
        {
            // Validate input parameters
            if (chars == null || bytes == null)
            {
#if EXCEPTION_STRINGS
                throw new ArgumentNullException(chars == null ? "chars" : "bytes", Environment.GetResourceString("ArgumentNull_Array"));
#else
                throw new ArgumentNullException();
#endif
            }

            if (byteCount < 0 || charCount < 0)
            {
#if EXCEPTION_STRINGS
                throw new ArgumentOutOfRangeException((byteCount<0 ? "byteCount" : "charCount"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
#else
                throw new ArgumentOutOfRangeException();
#endif
            }

            // We don't want to throw
            this.m_mustFlush = flush;
            this.m_throwOnOverflow = false;
            this.m_bytesUsed = 0;

            // Do conversion
            charsUsed = this.m_encoding.GetChars(bytes, byteCount, chars, charCount, this);
            bytesUsed = this.m_bytesUsed;

            // Its completed if they've used what they wanted AND if they didn't want flush or if we are flushed
            completed = (bytesUsed == byteCount) && (!flush || !this.HasState) && (m_fallbackBuffer == null || m_fallbackBuffer.Remaining == 0);

            // Our data thingys are now full, we can return
        }

        public bool MustFlush
        {
            get
            {
                return m_mustFlush;
            }
        }

        // Anything left in our decoder?
        internal virtual bool HasState
        {
            get
            {
                return false;
            }
        }

        // Allow encoding to clear our must flush instead of throwing (in ThrowCharsOverflow)
        internal void ClearMustFlush()
        {
            m_mustFlush = false;
        }
    }
}
