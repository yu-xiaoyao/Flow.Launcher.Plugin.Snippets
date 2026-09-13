namespace TestCodeProject
{
    internal static class Program
    {
        [STAThread]
        static int Main()
        {
            SnippetsHarness.SetLogger();
            QueryFactory.DumpApi();

            var ok = FolderQueryParserTests.Run();
            ok &= FolderSearchModeTests.Run();
            ok &= FolderParamPositionTests.Run();
            ok &= FuzzTests.Run();
            ok &= SqliteLikeTests.Run();
            ok &= RealDbIntegrationTests.Run();

            Console.WriteLine();
            Console.WriteLine(ok ? "==== 结论: SPEC 全部通过 ====" : "==== 结论: 有 SPEC 用例失败 ====");
            return ok ? 0 : 1;
        }
    }
}
