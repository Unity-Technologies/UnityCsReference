// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Net;
using System.Runtime.InteropServices;

namespace UnityEngine.Networking
{
    public sealed partial class NetworkTransport
    {
        internal static bool DoesEndPointUsePlatformProtocols(EndPoint endPoint)
        {
            if (endPoint.GetType().FullName == "UnityEngine.PS4.SceEndPoint" || endPoint.GetType().FullName == "UnityEngine.PSVita.SceEndPoint")
            {
                SocketAddress src = endPoint.Serialize();

                // is vport area non zero?
                if ((src[8] != 0) || (src[9] != 0))
                    return true;
            }
            return false;
        }

        //function will return connection id for this connect request
        ///PopData with eventtype ConnectEvent will be signal that this slot is successfully connected
        ///exceptionConnectionId defines connectionId with exception tuning params (returned from AddConfigException)
        ///exceptionConnectionId==0 means default connection
        ///return connectionId if success 0 otherwise
        public static int ConnectEndPoint(int hostId, EndPoint endPoint, int exceptionConnectionId, out byte error)
        {
            error = 0;

            // This class is implemented in the Xbox One Plugins API
            const string kXboxOneEndPointClass = "UnityEngine.XboxOne.XboxOneEndPoint";
            // XboxOneEndPoint validation constants
            byte[] XboxOneEndPointPacketSignature = new byte[] { 0x5f, 0x24, 0x13, 0xf6 }; // Our magic signature (it's a constant we randomly-generated)
            const int kXboxOneEndPointPacketSize = 2 + 4 + 8; // sizeof(AddressFamily) + XboxOneEndPointPacketSignature.Length + 8 bytes(64bit pointer)
            const int kSDASocketStorageOffset = 6; // sizeof(AddressFamily) + XboxOneEndPointPacketSignature.Length
            const int kSockAddrStorageLength = 128; // sizeof(sockaddr_storage)

            if (endPoint == null) // We require an XboxOneEndPoint to continue
                throw new NullReferenceException("Null EndPoint provided");
            if ((endPoint.GetType().FullName != kXboxOneEndPointClass) && (endPoint.GetType().FullName != "UnityEngine.PS4.SceEndPoint") && (endPoint.GetType().FullName != "UnityEngine.PSVita.SceEndPoint"))
                throw new ArgumentException("Endpoint of type XboxOneEndPoint or SceEndPoint  required");

            if (endPoint.GetType().FullName == kXboxOneEndPointClass)
            {
                EndPoint xboxOneEndPoint = endPoint;
                if (xboxOneEndPoint.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6)
                    throw new ArgumentException("XboxOneEndPoint has an invalid family");

                // Okay, now serialise the endpoint, and convert it to a normal byte[] buffer
                SocketAddress src = xboxOneEndPoint.Serialize();

                if (src.Size != kXboxOneEndPointPacketSize) // The specified socket is not an XboxEndPoint (wrong size!)
                    throw new ArgumentException("XboxOneEndPoint has an invalid size");

                if (src[0] != 0 || src[1] != 0)
                    throw new ArgumentException("XboxOneEndPoint has an invalid family signature");
                if (src[2] != XboxOneEndPointPacketSignature[0] ||
                    src[3] != XboxOneEndPointPacketSignature[1] ||
                    src[4] != XboxOneEndPointPacketSignature[2] ||
                    src[5] != XboxOneEndPointPacketSignature[3])
                    throw new ArgumentException("XboxOneEndPoint has an invalid signature");

                // Okay, now extract the pointer to the SOCKET_STORAGE address to a byte array
                byte[] dst = new byte[8]; // 8 bytes (64bit pointer)
                for (int i = 0; i < dst.Length; ++i)
                {
                    dst[i] = src[kSDASocketStorageOffset + i];
                }

                // Convert the byte[] pointer to an IntPtr
                IntPtr st = new IntPtr(BitConverter.ToInt64(dst, 0));
                if (st == IntPtr.Zero)
                    throw new ArgumentException("XboxOneEndPoint has an invalid SOCKET_STORAGE pointer");

                byte[] SocketAddressFamily = new byte[2]; // short
                System.Runtime.InteropServices.Marshal.Copy(st, SocketAddressFamily, 0, SocketAddressFamily.Length);

                System.Net.Sockets.AddressFamily a = (System.Net.Sockets.AddressFamily)((((int)SocketAddressFamily[1]) << 8) + (int)SocketAddressFamily[0]);
                if (a != System.Net.Sockets.AddressFamily.InterNetworkV6)
                    throw new ArgumentException("XboxOneEndPoint has corrupt or invalid SOCKET_STORAGE pointer");
                return Internal_ConnectEndPoint(hostId, st, kSockAddrStorageLength, exceptionConnectionId, out error);
            }
            else
            {
                const int kSceEndPointPacketSize = 16;
                const int kSceSockAddrSize = 16;
                const int kSCE_NET_AF_INET = 2; // from <socket.h>
                // Sony platforms network code
                SocketAddress src = endPoint.Serialize();

                if (src.Size != kSceEndPointPacketSize) // The specified socket is not an XboxEndPoint (wrong size!)
                    throw new ArgumentException("EndPoint has an invalid size");

                if (src[0] != src.Size)
                    throw new ArgumentException("EndPoint has an invalid size value");

                if (src[1] != kSCE_NET_AF_INET)
                    throw new ArgumentException("EndPoint has an invalid family value");

                byte[] dst = new byte[kSceSockAddrSize];
                for (int i = 0; i < dst.Length; ++i)
                {
                    dst[i] = src[i];
                }

                IntPtr unmanagedPointer = Marshal.AllocHGlobal(dst.Length);
                Marshal.Copy(dst, 0, unmanagedPointer, dst.Length);
                int result = Internal_ConnectEndPoint(hostId, unmanagedPointer, kSceSockAddrSize, exceptionConnectionId, out error);
                Marshal.FreeHGlobal(unmanagedPointer);
                return result;
            }
        }
    } // NetworkTransport class
} // UnityEngine.Networking namespace

