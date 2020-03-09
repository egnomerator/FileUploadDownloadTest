using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace FileUploadDownloadTest.Utilities
{
    public class FileHelpers
    {
        public static async Task<byte[]> ProcessStreamedFile(
            MultipartSection section, ContentDispositionHeaderValue contentDisposition,
            ModelStateDictionary modelState, string[] permittedExtensions, long sizeLimit)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    await section.Body.CopyToAsync(memoryStream);

                    // Check if the file is empty or exceeds the size limit.
                    if (false /*memoryStream.Length == 0*/)
                    {
                        modelState.AddModelError("File", "The file is empty.");
                    }
                    else if (false /*memoryStream.Length > sizeLimit*/)
                    {
                        var megabyteSizeLimit = sizeLimit / 1048576;
                        modelState.AddModelError("File",
                            $"The file exceeds {megabyteSizeLimit:N1} MB.");
                    }
                    else if (false/*!IsValidFileExtensionAndSignature(contentDisposition.FileName.Value, memoryStream, permittedExtensions)*/)
                    {
                        modelState.AddModelError("File",
                            "The file type isn't permitted or the file's " +
                            "signature doesn't match the file's extension.");
                    }
                    else
                    {
                        return memoryStream.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                modelState.AddModelError("File",
                    "The upload failed. Please contact the Help Desk " +
                    $" for support. Error: {ex.HResult}");
                // Log the exception
            }

            return new byte[0];
        }
    }
}
