using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FileUploadDownloadTestWebSite.Filters;
using FileUploadDownloadTestWebSite.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace FileUploadDownloadTestWebSite.Controllers
{
    public class FileUploadController : Controller
    {
        private string _storageDirectory = @"";
        // Get the default form options so that we can use them to set the default 
        // limits for request body data.
        private static readonly FormOptions _defaultFormOptions = new FormOptions();
        private readonly long _fileSizeLimit;
        private readonly string[] _permittedExtensions = { ".txt" };

        public FileUploadController()
        {
            
        }

        public async Task<IActionResult> FileDownload()
        {
            throw new NotImplementedException("need this");
        }

        [DisableFormValueModelBinding]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> FileUpload(CancellationToken token)
        {
            var contentType = Request.ContentType;
            if (string.IsNullOrWhiteSpace(contentType) ||
                !contentType.Contains("multipart/", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest();
            }

            var boundary = MultipartRequestHelper.GetBoundary(MediaTypeHeaderValue.Parse(Request.ContentType), _defaultFormOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, Request.Body);
            var section = await reader.ReadNextSectionAsync(token);

            while (section != null)
            {
                var hasContentDispositionHeader =
                    ContentDispositionHeaderValue.TryParse(
                        section.ContentDisposition, out var contentDisposition);
                if (!hasContentDispositionHeader) return BadRequest();

                //var trustedFileNameForDisplay = WebUtility.HtmlEncode(
                //    contentDisposition.FileName.Value);
                //var trustedFileNameForFileStorage = Path.GetRandomFileName();

                var fileName = contentDisposition.FileName.Value;
                if(fileName == null) break;

                var filePath = _storageDirectory;
                var fullPath = Path.Combine(filePath, fileName);

                #region (approx. 0:35) fastest ... and better memory ... i think?? did i see it spike right at the end??

                var streamedFileContent = await FileHelpers.ProcessStreamedFile(
                    section, contentDisposition, ModelState,
                    _permittedExtensions, _fileSizeLimit);

                await using (var targetStream = System.IO.File.Create(
                    Path.Combine(filePath, fileName)))
                {
                    await targetStream.WriteAsync(streamedFileContent);

                    //_logger.LogInformation(
                    //    "Uploaded file '{TrustedFileNameForDisplay}' saved to " +
                    //    "'{TargetFilePath}' as {TrustedFileNameForFileStorage}",
                    //    trustedFileNameForDisplay, _targetFilePath,
                    //    trustedFileNameForFileStorage);
                }

                #endregion

                #region (approx. 1:20) chunking1024 buffer - great memory but slow

                const int chunk = 1024*1024;
                var buffer = new byte[chunk];
                var bytesRead = 0;

                await using (var stream = new FileStream(fullPath, FileMode.Append))
                {
                    do
                    {
                        bytesRead = await section.Body.ReadAsync(buffer, 0, buffer.Length, token);
                        await stream.WriteAsync(buffer, 0, bytesRead, token);
                    } while (bytesRead > 0);
                }


                #endregion

                #region (approx. 0:40) IFormFile fast but bad on memory - buffers all into memory



                #endregion

                // Drain any remaining section body that hasn't been consumed and
                // read the headers for the next section.
                section = await reader.ReadNextSectionAsync(token);
            }

            return Ok();
        }

        [DisableRequestSizeLimit]
        public async Task<IActionResult> FileUploadOld(IEnumerable<IFormFile> files)
        {
            var fileCount = 0;
            var totalSize = 0d;

            var filePaths = new List<string>();
            foreach (var formFile in files)
            {
                if (formFile.Length > 0)
                {
                    totalSize = totalSize + formFile.Length;
                    fileCount++;
                    // full path to file in temp location
                    var filePath = Path.Combine(@"D:\_Dan\Docs\__ work-hub __\testing\storage", formFile.FileName);  // Path.GetTempFileName(); //we are using Temp file name just for the example. Add your own file path.
                    filePaths.Add(filePath);

                    await using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await formFile.CopyToAsync(stream);
                    }
                }
            }

            // process uploaded files
            // Don't rely on or trust the FileName property without validation.


            return Ok(new { count = fileCount, filePaths });
        }

        [HttpPost]
        [DisableFormValueModelBinding]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPhysical()
        {
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                ModelState.AddModelError("File",
                    $"The request couldn't be processed (Error 1).");
                // Log error

                return BadRequest(ModelState);
            }

            var boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(Request.ContentType),
                _defaultFormOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);
            var section = await reader.ReadNextSectionAsync();

            while (section != null)
            {
                var hasContentDispositionHeader =
                    ContentDispositionHeaderValue.TryParse(
                        section.ContentDisposition, out var contentDisposition);

                if (hasContentDispositionHeader)
                {
                    // This check assumes that there's a file
                    // present without form data. If form data
                    // is present, this method immediately fails
                    // and returns the model error.
                    if (!MultipartRequestHelper
                        .HasFileContentDisposition(contentDisposition))
                    {
                        ModelState.AddModelError("File",
                            $"The request couldn't be processed (Error 2).");
                        // Log error

                        return BadRequest(ModelState);
                    }
                    else
                    {
                        // Don't trust the file name sent by the client. To display
                        // the file name, HTML-encode the value.
                        var trustedFileNameForDisplay = WebUtility.HtmlEncode(
                                contentDisposition.FileName.Value);
                        var trustedFileNameForFileStorage = Path.GetRandomFileName();

                        // **WARNING!**
                        // In the following example, the file is saved without
                        // scanning the file's contents. In most production
                        // scenarios, an anti-virus/anti-malware scanner API
                        // is used on the file before making the file available
                        // for download or for use by other systems. 
                        // For more information, see the topic that accompanies 
                        // this sample.

                        var streamedFileContent = await FileHelpers.ProcessStreamedFile(
                            section, contentDisposition, ModelState,
                            _permittedExtensions, _fileSizeLimit);

                        if (!ModelState.IsValid)
                        {
                            return BadRequest(ModelState);
                        }

                        using (var targetStream = System.IO.File.Create(
                            Path.Combine(_storageDirectory, trustedFileNameForFileStorage)))
                        {
                            await targetStream.WriteAsync(streamedFileContent);

                            //_logger.LogInformation(
                            //    "Uploaded file '{TrustedFileNameForDisplay}' saved to " +
                            //    "'{TargetFilePath}' as {TrustedFileNameForFileStorage}",
                            //    trustedFileNameForDisplay, _targetFilePath,
                            //    trustedFileNameForFileStorage);
                        }
                    }
                }

                // Drain any remaining section body that hasn't been consumed and
                // read the headers for the next section.
                section = await reader.ReadNextSectionAsync();
            }

            return Created(nameof(FileUploadController), null);
        }
    }
}
