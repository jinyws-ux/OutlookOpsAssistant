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

            TicketCreateResult result =
                new TicketCreateResult
                {
                    Success = true,
                    TicketId = ticketId,
                    Message = "模拟工单创建成功",
                    CompletedAt = DateTime.Now
                };

            string vin;
            if (request.Parameters.TryGetValue(
                    "vin",
                    out vin))
            {
                result.Data["vin"] = vin;
            }

            string site;
            if (request.Parameters.TryGetValue(
                    "site",
                    out site))
            {
                result.Data["site"] = site;
            }

            return result;
        }
    }
}