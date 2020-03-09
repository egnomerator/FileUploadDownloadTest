using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FileUploadDownloadTest.Filters;
using FileUploadDownloadTest.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Microsoft.Win32.SafeHandles;

namespace FileUploadDownloadTest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileStreamController : ControllerBase
    {
        private const string StorageDirectory = @"\\set\path\to\storage\location";
        // Get the default form options so that we can use them to set the default 
        // limits for request body data.
        private static readonly FormOptions _defaultFormOptions = new FormOptions();
        private readonly long _fileSizeLimit;
        private readonly string[] _permittedExtensions = { ".txt" };

        public FileStreamController()
        {
            
        }

        // GET https://localhost:5000/api/filestream

        [HttpGet]
        public async Task<IActionResult> Test()
        {
            const string existingFileName = "file-name-of-previously-uploaded-file.mp3";
            var fullPath = Path.Combine(StorageDirectory, existingFileName);

            var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            return File(fs, "audio/mpeg", "test.mp3");
        }

        // POST https://localhost:5000/api/filestream

        [DisableFormValueModelBinding]
        [DisableRequestSizeLimit]
        [HttpPost]
        public async Task<IActionResult> FileUpload(CancellationToken token)
        {
            var contentType = Request.ContentType;
            if (string.IsNullOrWhiteSpace(contentType) ||
                !contentType.Contains("multipart/", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest();
            }

            //throw new Exception("oops");

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
                if (string.IsNullOrWhiteSpace(fileName)) return BadRequest();

                var fullPath = Path.Combine(StorageDirectory, fileName);

                #region (approx. 0:35) fastest ... and better memory ... i think?? did i see it spike right at the end??

                //var streamedFileContent = await FileHelpers.ProcessStreamedFile(
                //    section, contentDisposition, ModelState,
                //    _permittedExtensions, _fileSizeLimit);

                //await using (var targetStream = System.IO.File.Create(Path.Combine(filePath, fileName)))
                //{
                //    await targetStream.WriteAsync(streamedFileContent);

                //    //_logger.LogInformation(
                //    //    "Uploaded file '{TrustedFileNameForDisplay}' saved to " +
                //    //    "'{TargetFilePath}' as {TrustedFileNameForFileStorage}",
                //    //    trustedFileNameForDisplay, _targetFilePath,
                //    //    trustedFileNameForFileStorage);
                //}

                #endregion

                #region (approx. 1:20) chunking1024 buffer - great memory but slow

                const int chunk = 1024 * 1024;
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



        // ------------------------------------------------------------------------------------------------------------------------------------------



        public async Task<IActionResult> UploadFileByStream(IFormFile file)
        {
            return Ok();
        }

        public async Task<IActionResult> UploadFileByStream2()
        {
            if (!IsMultipartContentType(Request.ContentType))
            {
                return BadRequest();
            }

            var boundary = GetBoundary(Request.ContentType);
            var reader = new MultipartReader(boundary, Request.Body);
            var section = await reader.ReadNextSectionAsync();

            while (section != null)
            {
                // process each image
                const int chunkSize = 1024;
                var buffer = new byte[chunkSize];
                var bytesRead = 0;
                var fileName = GetFileName(section.ContentDisposition);

                await using (var stream = new FileStream(fileName, FileMode.Append))
                {
                    do
                    {
                        bytesRead = await section.Body.ReadAsync(buffer, 0, buffer.Length);
                        stream.Write(buffer, 0, bytesRead);

                    } while (bytesRead > 0);
                }

                section = await reader.ReadNextSectionAsync();
            }

            return Ok();
        }

        private static bool IsMultipartContentType(string contentType)
        {
            return
                !string.IsNullOrEmpty(contentType) &&
                contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetBoundary(string contentType)
        {
            var elements = contentType.Split(' ');
            var element = elements.First(entry => entry.StartsWith("boundary="));
            var boundary = element.Substring("boundary=".Length);
            // Remove quotes
            if (boundary.Length >= 2 && boundary[0] == '"' &&
                boundary[boundary.Length - 1] == '"')
            {
                boundary = boundary.Substring(1, boundary.Length - 2);
            }
            return boundary;
        }

        private string GetFileName(string contentDisposition)
        {
            return contentDisposition
                .Split(';')
                .SingleOrDefault(part => part.Contains("filename"))
                .Split('=')
                .Last()
                .Trim('"');
        }
    }
}