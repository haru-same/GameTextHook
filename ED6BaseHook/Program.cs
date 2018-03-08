using HookUtils;

namespace ED6BaseHook
{
    class Program
    {
        static void Main(string[] args)
        {
            var processHandle = ProcessUtil.OpenProcess("ed6_win_DX9");
            ED6Util.ED6Monitor(processHandle);
        }
    }
}
