import re

content = open(r'C:\Users\Kaiiske\Pictures\CorvusIAM-testes\CorvusIAM\Controllers\AdminController.cs', 'r', encoding='utf-8').read()

new_methods = """
        private static readonly string _integracoesFilePath = "integracoes.json";
        
        private List<ModuloStatus> GetIntegracoes()
        {
            if (System.IO.File.Exists(_integracoesFilePath))
            {
                var json = System.IO.File.ReadAllText(_integracoesFilePath);
                return System.Text.Json.JsonSerializer.Deserialize<List<ModuloStatus>>(json) ?? new List<ModuloStatus>();
            }
            return new List<ModuloStatus>();
        }

        private void SaveIntegracoes(List<ModuloStatus> integracoes)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(integracoes);
            System.IO.File.WriteAllText(_integracoesFilePath, json);
        }

        [Authorize(Policy = "RequireAdmin")]
        public IActionResult Integracoes()
        {
            return View(GetIntegracoes());
        }

        [HttpPost]
        [Authorize(Policy = "RequireAdmin")]
        public IActionResult AddIntegracao([FromBody] ModuloStatus model)
        {
            var list = GetIntegracoes();
            list.Add(model);
            SaveIntegracoes(list);
            return Ok(new { message = "Integração adicionada" });
        }

        [HttpPost]
        [Authorize(Policy = "RequireAdmin")]
        public IActionResult RemoveIntegracao([FromBody] string nome)
        {
            var list = GetIntegracoes();
            list.RemoveAll(i => i.Nome == nome);
            SaveIntegracoes(list);
            return Ok(new { message = "Integração removida" });
        }
"""

content = content.replace("Modulos = new List<ModuloStatus>()", "Modulos = GetIntegracoes()")
content = content.replace("public IActionResult Index()", new_methods + "\n        [Authorize(Policy = \"RequireAdmin\")]\n        public IActionResult Index()")

open(r'C:\Users\Kaiiske\Pictures\CorvusIAM-testes\CorvusIAM\Controllers\AdminController.cs', 'w', encoding='utf-8').write(content)
