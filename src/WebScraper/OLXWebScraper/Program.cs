using HtmlAgilityPack;
using OLXWebScraper.Class;
using OLXWebScraper.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace OLXWebScraper
{
    class Program
    {
        //Busca no RJ
        private const String URL_BASE = "https://rj.olx.com.br/rio-de-janeiro-e-regiao?q={0}";

        static void Main(string[] args)
        {
            try
            {
                var anunciosEncontrados = new List<Anuncio>();
                List<String> CodigosAnunciosExistentes = new List<String>();

                //Arquivo contendo a lista de palavras que devem ser monitoradas, colocar uma expressão por linha
                var palavras = File.ReadAllLines(Path.Combine(Directory.GetCurrentDirectory(), "palavras.txt"));
                //Arquivo que armazena os códigos já procurados, servirá como um banco de dados em arquivo texto
                var arquivoAnuncioExistentes = Path.Combine(Directory.GetCurrentDirectory(), "anuncios.txt");

                //Se o arquivo com os códigos existir, carrega para a memória
                if (File.Exists(arquivoAnuncioExistentes))
                {
                    CodigosAnunciosExistentes = File.ReadAllLines(arquivoAnuncioExistentes).ToList();
                }

                //Varre a lista de expressões para busca
                foreach (var linha in palavras)
                {
                    //Remove os espaços do começo e do final da expressão
                    var palavraAuxiliar = linha.TrimStart().TrimEnd();
                    //Caso tenha espaço no meio, adiciona o encoding HTML de espaço
                    var expressao = palavraAuxiliar.Replace(" ", "%20");

                    //Abre um webclient com a URL mais a expressão de busca
                    using (var wc = new WebClient())
                    {
                        String resultado = wc.DownloadString(String.Format(URL_BASE, expressao));

                        var documento = new HtmlAgilityPack.HtmlDocument();
                        documento.LoadHtml(resultado);

                        HtmlNode elemento = documento.GetElementbyId("ad-list");

                        if (elemento != null)
                        {
                            anunciosEncontrados = ObterListaDeNovosAnuncios(elemento, CodigosAnunciosExistentes);
                        }
                    }

                    //Se encontrou algum anúncio novo, salva o código no TXT e envia por e-mail
                    if (anunciosEncontrados.Count > 0)
                    {
                        foreach (var anuncioEncontrado in anunciosEncontrados)
                        {
                            //Adiciona uma linha no arquivo com o código do anúncio
                            using (StreamWriter w = File.AppendText(arquivoAnuncioExistentes))
                            {
                                w.WriteLine(anuncioEncontrado.Codigo);
                            }
                        }

                        EnviarEmailAnunciosEncontrados(anunciosEncontrados, linha);
                    }
                }
            }
            catch (Exception ex)
            {
                //To-do: Programar pra gravar no log de eventos do windows
                Console.WriteLine("Deu ruim: " + ex.Message);                
            }
        }

        private static void EnviarEmailAnunciosEncontrados(List<Anuncio> anunciosEncontrados, String expressao)
        {
            var assuntoEmail = "[OLX Scrapper] Anúncios encontrados para a expresão: " + expressao +  " - " + String.Format("{0:dd/MM/yyyy HH:mm}", DateTime.Now);
            StringBuilder SBMensagem = new StringBuilder();

            foreach (var anuncio in anunciosEncontrados)
            {
                SBMensagem.AppendLine("Código: " + anuncio.Codigo);
                SBMensagem.AppendLine("Título: " + anuncio.Titulo);
                SBMensagem.AppendLine("Link: " + "<a href= \"" + anuncio.Link + "\"" + " target=\"_blank\">" + " Clique</a>");
                SBMensagem.AppendLine("Valor: " + anuncio.Valor);
                SBMensagem.AppendLine("Data da Publicação: " + anuncio.DataPublicacao);
                SBMensagem.AppendLine("---------------------------------------------");
            }

            var destinatario = Environment.GetEnvironmentVariable("OLX_SCRAPPER_DESTINATARIO");
            var host = Environment.GetEnvironmentVariable("OLX_SCRAPPER_HOST_EMAIL");
            var porta = Environment.GetEnvironmentVariable("OLX_SCRAPPER_PORTA_EMAIL");
            Int32.TryParse(porta, out int PortaInt);
            var usuario = Environment.GetEnvironmentVariable("OLX_SCRAPPER_USUARIO_EMAIL");
            var remetente = Environment.GetEnvironmentVariable("OLX_SCRAPPER_REMETENTE_EMAIL");
            var senha = Environment.GetEnvironmentVariable("OLX_SCRAPPER_SENHA_EMAIL");

            EmailService.EnviarEmail(assuntoEmail, SBMensagem.ToString());
        }

        private static List<Anuncio> ObterListaDeNovosAnuncios(HtmlNode elemento, List<string> codigosAnunciosExistentes)
        {
            List<Anuncio> AnunciosEncontrados = new List<Anuncio>();

            foreach (var node in elemento.ChildNodes)
            {
                //To-do: Buscar pelo XPath
                if (!String.IsNullOrEmpty(node.InnerHtml))
                {
                    var titulo = node.ChildNodes[0].Attributes["title"].Value;
                    var codigo = node.ChildNodes[0].Attributes["data-lurker_list_id"].Value;
                    var link = node.ChildNodes[0].Attributes["href"].Value;
                    var valor = String.Empty;
                    try
                    {
                        valor = node.ChildNodes[0].ChildNodes[0].ChildNodes[1].ChildNodes[0].ChildNodes[1].ChildNodes[1].ChildNodes[0].ChildNodes[0].ChildNodes[0].InnerText;
                    }
                    catch (Exception)
                    {
                        valor = "Produto sem valor";
                    }

                    var dataPublicacao = node.ChildNodes[0].ChildNodes[0].ChildNodes[1].ChildNodes[0].ChildNodes[1].ChildNodes[3].ChildNodes[0].InnerText;

                    //Se o anúncio não existe no txt, adiciona na lista
                    if (!codigosAnunciosExistentes.Contains(codigo))
                    {
                        var anuncio = new Anuncio();
                        anuncio.Codigo = codigo;
                        anuncio.Titulo = titulo;
                        anuncio.Link = link;
                        anuncio.Valor = valor;
                        anuncio.DataPublicacao = dataPublicacao;

                        AnunciosEncontrados.Add(anuncio);
                    }
                }

            }

            return AnunciosEncontrados;
        }
    }
}
