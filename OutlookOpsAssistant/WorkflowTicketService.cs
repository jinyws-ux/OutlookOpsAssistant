using System;
using System.Collections.Generic;
using System.IO;

namespace OutlookOpsAssistant
{
    /// <summary>
    /// 负责串联：Helix 开单、当前邮件导出和附件上传、Case API 执行。
    /// 各步骤的具体实现由独立服务提供。
    /// </summary>
    public sealed class WorkflowTicketService : ITicketService
    {
        private readonly IHelixService helixService;
        private readonly ICaseApiService caseApiService;
        private readonly IMailExporter mailExporter;
        private readonly ApiConfiguration apiConfiguration;

        public WorkflowTicketService(
            IHelixService helixService,
            ICaseApiService caseApiService,
            IMailExporter mailExporter,
            ApiConfiguration apiConfiguration)
        {
            this.helixService =
                helixService ??
                throw new ArgumentNullException(
                    nameof(helixService));

            this.caseApiService =
                caseApiService ??
                throw new ArgumentNullException(
                    nameof(caseApiService));

            this.mailExporter = mailExporter;
            this.apiConfiguration =
                apiConfiguration ??
                new ApiConfiguration();
        }

        public TicketCreateResult CreateTicket(
            CaseDefinition caseDefinition,
            TicketCreateRequest request)
        {
            if (caseDefinition == null)
            {
                throw new ArgumentNullException(
                    nameof(caseDefinition));
            }

            if (request == null)
            {
                throw new ArgumentNullException(
                    nameof(request));
            }

            TicketCreateResult result =
                CreateInitialResult(request);

            List<string> messages =
                new List<string>();

            HelixTicketCreateResult helixResult =
                helixService.CreateTicket(request);

            if (!helixResult.Success)
            {
                result.Success = false;
                result.Message =
                    string.IsNullOrWhiteSpace(
                        helixResult.Message)
                        ? "Helix 工单创建失败。"
                        : helixResult.Message;
                result.CompletedAt = DateTime.Now;
                result.Data["helixStatus"] =
                    "创建失败";
                return result;
            }

            result.Success = true;
            result.TicketId = helixResult.TicketId;
            result.Data["helixStatus"] =
                "工单已创建";
            messages.Add(
                string.IsNullOrWhiteSpace(
                    helixResult.Message)
                    ? "Helix 工单创建成功"
                    : helixResult.Message);

            CopyData(
                helixResult.Data,
                result.Data);

            HandleMailAttachment(
                result,
                messages);

            HandleCaseApi(
                caseDefinition,
                request,
                result,
                messages);

            result.Message =
                string.Join("；", messages);
            result.CompletedAt = DateTime.Now;

            return result;
        }

        private TicketCreateResult CreateInitialResult(
            TicketCreateRequest request)
        {
            TicketCreateResult result =
                new TicketCreateResult
                {
                    Success = false,
                    CompletedAt = DateTime.Now
                };

            foreach (KeyValuePair<string, string> pair
                     in request.Parameters)
            {
                result.Data[pair.Key] = pair.Value;
            }

            return result;
        }

        private void HandleMailAttachment(
            TicketCreateResult result,
            ICollection<string> messages)
        {
            HelixConfiguration helixConfiguration =
                apiConfiguration.Helix ??
                new HelixConfiguration();

            if (!helixConfiguration.AttachCurrentMail)
            {
                result.Data["mailAttachmentStatus"] =
                    "未启用";
                return;
            }

            if (mailExporter == null)
            {
                result.Data["mailAttachmentStatus"] =
                    "未找到邮件导出服务";
                messages.Add(
                    "工单已创建，但当前邮件未上传");
                return;
            }

            ExportedMailFile exportedMail = null;

            try
            {
                string exportDirectory =
                    ResolveExportDirectory(
                        helixConfiguration.MailExportDirectory);

                exportedMail =
                    mailExporter.ExportCurrentMail(
                        exportDirectory);

                result.Data["mailFileName"] =
                    exportedMail.FileName;
                result.Data["mailFileSize"] =
                    exportedMail.Size;

                HelixAttachmentUploadResult uploadResult =
                    helixService.UploadAttachment(
                        result.TicketId,
                        exportedMail.FilePath);

                if (uploadResult.Success)
                {
                    result.Data["mailAttachmentStatus"] =
                        "已上传";
                    result.Data["mailAttachmentId"] =
                        uploadResult.AttachmentId ??
                        string.Empty;
                    messages.Add(
                        string.IsNullOrWhiteSpace(
                            uploadResult.Message)
                            ? "当前邮件附件已上传"
                            : uploadResult.Message);
                }
                else
                {
                    result.Data["mailAttachmentStatus"] =
                        "上传失败";
                    result.Data["mailAttachmentError"] =
                        uploadResult.Message ??
                        string.Empty;
                    messages.Add(
                        "工单已创建，但邮件附件上传失败");
                }
            }
            catch (Exception ex)
            {
                result.Data["mailAttachmentStatus"] =
                    "导出或上传失败";
                result.Data["mailAttachmentError"] =
                    ex.Message;
                messages.Add(
                    "工单已创建，但邮件附件处理失败");
            }
            finally
            {
                if (helixConfiguration
                        .DeleteExportedMailAfterUpload &&
                    exportedMail != null &&
                    !string.IsNullOrWhiteSpace(
                        exportedMail.FilePath))
                {
                    TryDeleteFile(exportedMail.FilePath);
                }
            }
        }

        private void HandleCaseApi(
            CaseDefinition caseDefinition,
            TicketCreateRequest request,
            TicketCreateResult result,
            ICollection<string> messages)
        {
            bool shouldExecute =
                apiConfiguration.Enabled &&
                request.AutomationEnabled &&
                caseDefinition.Api != null &&
                caseDefinition.Api.Enabled;

            if (!shouldExecute)
            {
                result.Data["caseApiStatus"] =
                    "未启用";
                return;
            }

            CaseApiExecutionResult apiResult =
                caseApiService.Execute(
                    caseDefinition,
                    request,
                    result.TicketId);

            CopyData(
                apiResult.Data,
                result.Data);

            if (apiResult.Success)
            {
                result.Data["caseApiStatus"] =
                    "执行成功";
                messages.Add(
                    string.IsNullOrWhiteSpace(
                        apiResult.Message)
                        ? "Case API 执行成功"
                        : apiResult.Message);
                return;
            }

            result.Success = false;
            result.Data["caseApiStatus"] =
                "执行失败";
            result.Data["caseApiError"] =
                apiResult.Message ??
                string.Empty;
            messages.Add(
                "工单已创建，但 Case API 执行失败");
        }

        private static void CopyData(
            IDictionary<string, object> source,
            IDictionary<string, object> target)
        {
            if (source == null || target == null)
            {
                return;
            }

            foreach (KeyValuePair<string, object> pair
                     in source)
            {
                target[pair.Key] = pair.Value;
            }
        }

        private static string ResolveExportDirectory(
            string configuredDirectory)
        {
            string path =
                string.IsNullOrWhiteSpace(
                    configuredDirectory)
                    ? "%LOCALAPPDATA%\\OutlookOpsAssistant\\temp"
                    : configuredDirectory.Trim();

            path =
                Environment.ExpandEnvironmentVariables(
                    path);

            if (!Path.IsPathRooted(path))
            {
                string localAppData =
                    Environment.GetFolderPath(
                        Environment.SpecialFolder
                            .LocalApplicationData);

                path = Path.Combine(
                    localAppData,
                    "OutlookOpsAssistant",
                    path);
            }

            return Path.GetFullPath(path);
        }

        private static void TryDeleteFile(
            string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
                // 临时文件清理失败不影响工单结果。
            }
        }
    }
}
