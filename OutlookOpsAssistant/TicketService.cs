using System;

namespace OutlookOpsAssistant
{
    public interface ITicketService
    {
        TicketCreateResult CreateTicket(
            TicketCreateRequest request);
    }

    public sealed class MockTicketService : ITicketService
    {
        public TicketCreateResult CreateTicket(
            TicketCreateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(
                    nameof(request));
            }

            string ticketId =
                "TEST-INC-" +
                DateTime.Now.ToString(
                    "yyyyMMddHHmmssfff");

            return new TicketCreateResult
            {
                Success = true,
                TicketId = ticketId,
                Message = "模拟工单创建成功"
            };
        }
    }
}