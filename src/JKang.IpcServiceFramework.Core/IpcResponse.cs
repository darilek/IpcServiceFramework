using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;

namespace JKang.IpcServiceFramework
{
    public class IpcResponse
    {
        [JsonConstructor]
        private IpcResponse(bool succeed, object data, string failure, string failureDetails, bool userCodeFailure, byte[] exceptionData = null)
        {
            Succeed = succeed;
            Data = data;
            Failure = failure;
            FailureDetails = failureDetails;
            UserCodeFailure = userCodeFailure;
            ExceptionData = exceptionData;
        }

        public static IpcResponse Fail(string failure)
        {
            return new IpcResponse(false, null, failure, null, false);
        }

        public static IpcResponse Fail(Exception ex, bool includeDetails, bool userFailure = false)
        {
            string message = null;
            string details = null;

            if (!userFailure)
            {
                message = "Internal server error: ";
            }

            message += GetFirstUsableMessage(ex);

            if (includeDetails)
            {
                details = ex.ToString();

                using (var ms = new MemoryStream())
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(ms, ex);

                    return new IpcResponse(false, null, message, details, userFailure, ms.ToArray());

                }
            }

            return new IpcResponse(false, null, message, details, userFailure);
        }

        public static IpcResponse Success(object data)
        {
            return new IpcResponse(true, data, null, null, false);
        }

        public bool Succeed { get; }
        public object Data { get; }
        public string Failure { get; }
        public string FailureDetails { get; }
        public bool UserCodeFailure { get; set; }

        public byte[] ExceptionData { get; }

        public Exception GetException()
        {
            Exception ex = GeFirstUsableException();
            if (UserCodeFailure)
            {
                if (ex == null)
                    throw new IpcServerUserCodeException(Failure, FailureDetails);
                //throw new IpcServerUserCodeException(Failure, ex);
                throw ex;
            }

            if (ex == null)
                throw new IpcServerException(Failure, FailureDetails);
            //throw new IpcServerException(Failure, ex);
            throw ex;
        }

        private Exception GeFirstUsableException()
        {
            if (ExceptionData != null)
            {
                using (var ms = new MemoryStream(ExceptionData))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    var obj = bf.Deserialize(ms);
                    if (obj is Exception ex)
                    {
                        var e = ex;
                        while (e != null)
                        {
                            if (!(e is TargetInvocationException))
                            {
                                return e;
                            }

                            e = e.InnerException;
                        }
                        return ex;
                    }
                }
            }

            return null;
        }

        private static string GetFirstUsableMessage(Exception ex)
        {
            var e = ex;

            while (e != null)
            {
                if (!(e is TargetInvocationException))
                {
                    return e.Message;
                }

                e = e.InnerException;
            }

            return ex.Message;
        }
    }
}
