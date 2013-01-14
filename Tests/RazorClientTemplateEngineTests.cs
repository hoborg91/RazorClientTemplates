namespace RazorClientTemplates
{
    using System.Diagnostics;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class RazorClientTemplateEngineTests
    {
        private readonly string _templateSource;

        public RazorClientTemplateEngineTests()
        {
            //var path = Path.Combine(Environment.CurrentDirectory, @"Templates\Movie.cshtml");
            //_templateSource = File.ReadAllText(path);
            _templateSource = this.ReadTestTemplate();
        }

        [TestMethod]
        public void ShouldRenderClientTemplate()
        {
            Debug.Write(
                new RazorClientTemplateEngine().RenderClientTemplate(
                    _templateSource));
        }

        private string ReadTestTemplate()
        {
            using (
                var resourceStream =
                    this.GetType().Assembly.GetManifestResourceStream(
                        "RazorClientTemplates.Templates.Movie.cshtml"))
            {
                var length = (int) resourceStream.Length;
                var bytes = new byte[length];
                resourceStream.Read(
                    bytes,
                    0,
                    length);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
        }
    }
}
