using System.Reflection;
using Flow.Launcher.Plugin;

namespace TestCodeProject
{
    /// <summary>
    /// 假的 <see cref="IPublicAPI"/>。
    /// 只需要 <c>GetTranslation</c>：直接返回 key 本身，测试里断言 key 就够了。
    /// 其余成员一律返回默认值（测试不会调到）。
    /// </summary>
    public class FakeApiProxy : DispatchProxy
    {
        public static IPublicAPI Create() => DispatchProxy.Create<IPublicAPI, FakeApiProxy>();

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            if (targetMethod.Name == nameof(IPublicAPI.GetTranslation) && args is { Length: 1 })
                return args[0];

            return targetMethod.ReturnType.IsValueType
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
        }
    }
}
