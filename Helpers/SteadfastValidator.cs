namespace OrderManagementSystem.Helpers
{
    public static class SteadfastValidator
    {
        public static (bool isValid, string errorMessage) ValidateOrder(
            string recipientName,
            string recipientPhone,
            string recipientAddress,
            decimal codAmount)
        {
            // Name validation
            if (string.IsNullOrWhiteSpace(recipientName) || recipientName.Length > 100)
            {
                return (false, "Recipient name is required and must be within 100 characters");
            }

            // Phone validation (Bangladesh format)
            if (!System.Text.RegularExpressions.Regex.IsMatch(recipientPhone, @"^01\d{9}$"))
            {
                return (false, "Phone number must be 11 digits starting with 01");
            }

            // Address validation
            if (string.IsNullOrWhiteSpace(recipientAddress) || recipientAddress.Length > 250)
            {
                return (false, "Address is required and must be within 250 characters");
            }

            // COD amount validation
            if (codAmount < 0)
            {
                return (false, "COD amount cannot be negative");
            }

            return (true, string.Empty);
        }
    }
}