namespace DonationApp.Models;

public enum ItemCondition
{
    Baru,
    Bekas
}

public enum ItemCategory
{
    Pakaian,
    Elektronik,
    PerabotRumah = 2,
    Mainan = 4,
    AlatMusik = 6
}

public enum ItemStatus
{
    Available,
    Claimed
}

public enum ItemRequestStatus
{
    Open,
    Fulfilled,
    Expired,
    Closed
}

public enum TransactionStatus
{
    Pending,
    Accepted,
    Rejected,
    Shipped,
    Delivered
}

public enum MetodePengiriman
{
    BelumDipilih,
    Kurir,
    Pickup
}

public enum ConversationType
{
    Donation,
    RequestOffer
}

public enum ImageOwnerType
{
    Donation,
    Request,
    RequestOffer
}

public enum NotificationType
{
    ClaimRequest,
    ClaimAccepted,
    ClaimRejected,
    ItemShipped,
    ItemDelivered,
    NewOffer,
    OfferAccepted,
    OfferRejected,
    NewRating,
    Report,
    PointEarned
}

public enum ReportReason
{
    Spam,
    FakeItem,
    Inappropriate,
    Scam,
    Other
}

public enum ReportStatus
{
    Open,
    Reviewing,
    Resolved,
    Dismissed
}

public enum PointTransactionType
{
    DonationCompleted,
    SpendPoint
}