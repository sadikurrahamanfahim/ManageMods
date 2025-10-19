using System.Text.Json.Serialization;

namespace OrderManagementSystem.Models.ViewModels
{
    // Request Model
    public class SteadfastOrderRequest
    {
        [JsonPropertyName("invoice")]
        public string Invoice { get; set; } = string.Empty;

        [JsonPropertyName("recipient_name")]
        public string RecipientName { get; set; } = string.Empty;

        [JsonPropertyName("recipient_phone")]
        public string RecipientPhone { get; set; } = string.Empty;

        [JsonPropertyName("recipient_address")]
        public string RecipientAddress { get; set; } = string.Empty;

        [JsonPropertyName("cod_amount")]
        public decimal CodAmount { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [JsonPropertyName("alternative_phone")]
        public string? AlternativePhone { get; set; }

        [JsonPropertyName("recipient_email")]
        public string? RecipientEmail { get; set; }

        [JsonPropertyName("item_description")]
        public string? ItemDescription { get; set; }

        [JsonPropertyName("total_lot")]
        public int? TotalLot { get; set; }

        [JsonPropertyName("delivery_type")]
        public int? DeliveryType { get; set; } // 0 = home delivery, 1 = point delivery
    }

    // Response Model
    public class SteadfastOrderResponse
    {
        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("consignment")]
        public SteadfastConsignment? Consignment { get; set; }
    }

    public class SteadfastConsignment
    {
        [JsonPropertyName("consignment_id")]
        public long ConsignmentId { get; set; }

        [JsonPropertyName("invoice")]
        public string Invoice { get; set; } = string.Empty;

        [JsonPropertyName("tracking_code")]
        public string TrackingCode { get; set; } = string.Empty;

        [JsonPropertyName("recipient_name")]
        public string RecipientName { get; set; } = string.Empty;

        [JsonPropertyName("recipient_phone")]
        public string RecipientPhone { get; set; } = string.Empty;

        [JsonPropertyName("recipient_address")]
        public string RecipientAddress { get; set; } = string.Empty;

        [JsonPropertyName("cod_amount")]
        public decimal CodAmount { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("note")]
        public string? Note { get; set; }
    }

    // Status Response
    public class SteadfastStatusResponse
    {
        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("delivery_status")]
        public string DeliveryStatus { get; set; } = string.Empty;
    }

    // Balance Response
    public class SteadfastBalanceResponse
    {
        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("current_balance")]
        public decimal CurrentBalance { get; set; }
    }
}