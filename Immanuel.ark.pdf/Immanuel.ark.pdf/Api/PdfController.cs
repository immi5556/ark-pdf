using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Immanuel.ark.pdf.Api
{
    [Route("api/pdf/v1")]
    [ApiController]
    public class PdfController : ControllerBase
    {
        //public FileContentResult ConvertHtmlToPdf()
        //{
        //    string html = @"<page id='a4-doc' size='A4'>A4<div style='width: 100px; height: 100px; border: 1px solid red; position: absolute; left: 568px; top: 293.281px;' class='ina4'></div><div class='ina4' style='width: 100px; height: 100px; border: 1px solid red; position: absolute; left: 480.344px; top: 138.563px;'></div></page>";
        //    PdfSharp.Pdf.PdfDocument pdf = PdfSharp.Pdf. TheArtOfDev.HtmlRenderer.PdfSharp.PdfGenerator.GeneratePdf(html, PdfSharp.PageSize.Letter);
        //    //pdf.Save("document.pdf");
        //    byte[] arr = null;
        //    using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
        //    {
        //        pdf.Save(stream, true);
        //        arr = stream.ToArray();
        //    }

        //    return new FileContentResult(arr, "application/pdf");
        //}
    }
}
