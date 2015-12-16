//
// Copyright (c) Microsoft Corporation.    All rights reserved.
//

namespace System.Net.Sockets
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    public class SocketException : Exception
    {
        private int _errorCode;

        public SocketException(SocketError errorCode)
        {
            _errorCode = (int)errorCode;
        }

        public int ErrorCode
        {
            get { return _errorCode; }
        }

    }; // class SocketException

} // namespace System.Net

