using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using SendGrid;
using SendGrid.Helpers.Mail;
using TestPDF;
using FormCollection = System.Web.Mvc.FormCollection;
using Path = System.IO.Path;

namespace Even3Certificate.Controllers
{
    public class HomeController : Controller
    {
        private string imagePath = @"/Content/Images";
        /// <summary>
        /// Exibe página para confecção do certificado
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            ViewBag.ErrorReturn = "";
            ViewBag.SuccessReturn = "";
            return View();
        }

        /// <summary>
        /// Envia os dados do form da página via Post
        /// </summary>
        /// <param name="formCollection">inputs do formulário</param>
        /// <returns>retorna para a página com as mensagens de sucesso ou erro</returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Index(FormCollection formCollection)
        {
            //texto do certificado
            string textCertificate = Request.Form["textCertificate"];
            //texto com os dados de envio dos participantes
            string participantsText = Request.Form["participants"];
            //caminho da imagem de funcdo do certificado
            string backgroundPath = Request.Form["imagePath"];
            //imagem de fundo do certificado
            var file = HttpContext.Request.Files["backgroundFile"];

            if (textCertificate.Contains("{{nome}}"))
            {
                if (file.ContentLength != 0)
                {
                    //pega as linhas com os dados dos participantes
                    List<string> participantsRows =
                        participantsText.Split(new string[] {"\r\n"}, StringSplitOptions.None).ToList();

                    List<Task> tasksList = new List<Task>();
                    foreach (string participantRow in participantsRows)
                    {
                        //pega os dados de cada participante
                        List<string> participant =
                            participantRow.Split(new string[] {"\t"}, StringSplitOptions.None).ToList();
                        string certificateContent = textCertificate.Replace("{{nome}}", participant[0]);

                        //usa task para enviar os emails para ser mais rápido
                        Task task = new Task(() =>
                            SendMail(participant[3], participant[0], certificateContent, backgroundPath));
                        task.Start();
                        tasksList.Add(task);
                    }

                    Task.WaitAll(tasksList.ToArray());
                    //mensagem de envio feito com sucesso
                    ViewBag.SuccessReturn = "Os certificados foram enviados!";
                }
                else
                {
                    ViewBag.ErrorReturn = "É preciso selecionar uma imagem para o modelo do certificado.";
                }
            }
            else
            {
                ViewBag.ErrorReturn = "É preciso adicionar a tag {{nome}} ao texto do certificado.";
            }

            return View();
        }
        /// <summary>
        /// Envia os emails para os participantes
        /// </summary>
        /// <param name="mail">email do participante</param>
        /// <param name="name">nome do participante</param>
        /// <param name="textHtml">texto do certificado</param>
        /// <param name="backgroundPath">imagem de fundo do certificado</param>
        private void SendMail(string mail, string name, string textHtml, string backgroundPath)
        {
            string apiKey = "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";
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress("produto@even3.com.br", "Even3");
            var subject = "Certificado Even3";
            var to = new EmailAddress(mail, name);
            var plainTextContent = "";
            var htmlContent = "Segue seu certificado em anexo.";
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            //cria o arquivo pdf para anexar
            var file = Convert.ToBase64String(CreatePDF(textHtml, backgroundPath));
            msg.AddAttachment("Certificado.pdf", file);
            var response = client.SendEmailAsync(msg);
        }

        /// <summary>
        /// Cria o PDF que será anexado ao email
        /// </summary>
        /// <param name="html">html com o texto do certificado</param>
        /// <param name="backgroundPath">caminho da imagem de fundo do certificado</param>
        /// <returns>retorna um array de bytes com o PDF</returns>
        private Byte[] CreatePDF(string html, string backgroundPath)
        {
            try
            {
                //define o tamanho da página
                HtmlToPdfBuilder builder = new HtmlToPdfBuilder(iTextSharp.text.PageSize.A4);
                HtmlPdfPage first = builder.AddPage();
                first.AppendHtml(html);
                return builder.RenderPdf(backgroundPath);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Salva a imagem para apresentar ela ao usuário no momento da criação do certificado
        /// </summary>
        /// <returns>retorna o caminho relativo da imagem para ser apresentada no fundo de uma DIV</returns>
        [HttpPost]
        public JsonResult UploadImageBackground()
        {
            var fileSavePath = "";
            if (HttpContext.Request.Files.AllKeys.Any())
            {
                // Get the uploaded image from the Files collection
                var httpPostedFile = HttpContext.Request.Files["UploadedImage"];

                if (httpPostedFile != null)
                {
                    // Validate the uploaded image(optional)

                    // Get the complete file path
                    fileSavePath = Path.Combine(HttpContext.Server.MapPath("~"+imagePath), httpPostedFile.FileName);

                    // Save the uploaded file to "UploadedFiles" folder
                    httpPostedFile.SaveAs(fileSavePath);
                    imagePath += "/"+httpPostedFile.FileName;
                }
            }

            string[] imagePaths = new string[2];
            imagePaths[0] = imagePath;
            imagePaths[1] = fileSavePath;
            return Json(imagePaths);
        }
    }
}