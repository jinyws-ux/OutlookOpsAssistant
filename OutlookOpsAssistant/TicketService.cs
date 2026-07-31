namespace OutlookOpsAssistant
{
    /// <summary>
    /// 对界面暴露的统一流程入口。
    /// 具体实现可以串联 Helix、邮件附件和 Case API。
    /// </summary>
    public interface ITicketService
    {
        TicketCreateResult CreateTicket(
            CaseDefinition caseDefinition,
            TicketCreateRequest request);
    }
}
