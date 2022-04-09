using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shared;

public static class Helpers
{
    public static async Task<(bool success, string errorMessage)> Retry(Func<Task<bool>> action, int retryCount,
        int retryIntervalMs, CancellationToken cancellationToken)
    {
        string message = "";
        for (int i = 0; i < retryCount && !cancellationToken.IsCancellationRequested; ++i)
        {
            try
            {
                bool success = await action();
                if (success)
                {
                    return (true, message);
                }
            }
            catch (Exception e)
            {
                message = e.ToString();
            }
            await Task.Delay(retryIntervalMs);
        }

        return (false, message);
    }

    public static byte[] ToLittleEndian(this int length)
    {
        byte[] lengthEndian = BitConverter.GetBytes(length);
        if (!BitConverter.IsLittleEndian)
        {
            return new byte[] { lengthEndian[4], lengthEndian[3] };
        }

        return new byte[] { lengthEndian[0], lengthEndian[1] };
    }

    public static int ToInt16(this byte[] endianNum)
    {
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(endianNum);
        }

        return BitConverter.ToInt16(endianNum);
    }
}
