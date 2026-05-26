# Model Refactor — IDonasi

## Files changed

| File | Change |
|---|---|
| `ApplicationUser.cs` | Removed `NomorTelepon` (use `PhoneNumber`), added `IsAdmin`, `TotalPoin`, `CreatedAt`, made `KodePos` nullable |
| `ListingBase.cs` | **New** — abstract base for `Item` and `ItemRequest` |
| `Item.cs` | Now extends `ListingBase`, `ExpiresAt` default removed, `ItemCategory.Semua` removed |
| `ItemRequest.cs` | Now extends `ListingBase`, `KondisiMinimum` is now `ItemCondition?` (null = either) |
| `ItemImage.cs` | Unified — replaces `ItemImage`, `RequestImage`, `RequestOfferImage`. Has `OwnerType` discriminator and three nullable FKs |
| `ClaimRequest.cs` | Added nav to `Feedbacks` and `PointTransactions` |
| `RequestOffer.cs` | Images now use unified `ItemImage`, added nav to `Feedbacks` and `PointTransactions` |
| `Conversation.cs` | Added `Type` discriminator (`Donation`/`RequestOffer`), FKs are now nullable accordingly |
| `ChatMessage.cs` | `Content` no longer `[Required]`, `ImagePath` confirmed nullable |
| `Feedback.cs` | **Renamed** from `UserReputation` — covers both donation and request offer flows via nullable FKs |
| `Notification.cs` | Added `Type` (`NotificationType` enum) and nullable `RefId` |
| `Report.cs` | **New** — with `ReportReason` and `ReportStatus` enums |
| `AdminAction.cs` | **New** |
| `RequestLimit.cs` | **New** — `PeriodEnd` not stored, compute as `PeriodStart.AddDays(N)` |
| `PointTransaction.cs` | **New** — with `PointTransactionType` enum |
| `AppDbContext.cs` | Updated `DbSet`s, removed old image sets, added new sets, configured delete behaviors |

## Files to DELETE

```
Models/ItemImage.cs          ← replaced by unified ItemImage.cs
Models/RequestImage.cs       ← merged into ItemImage.cs
Models/RequestOfferImage.cs  ← merged into ItemImage.cs
Models/UserReputation.cs     ← replaced by Feedback.cs
```

## Migration notes

Run after dropping in the new files:
```bash
dotnet ef migrations add ModelRefactor
dotnet ef database update
```

### Breaking changes to handle in controllers

1. `UserReputation` → `Feedback` everywhere
   - `_db.UserReputations` → `_db.Feedbacks`
   - `ClaimRequest.Reputations` → `ClaimRequest.Feedbacks`

2. `RequestImage` / `RequestOfferImage` → `ItemImage`
   - `_db.RequestImages` → `_db.ItemImages` (with `OwnerType = ImageOwnerType.Request`)
   - `_db.RequestOfferImages` → `_db.ItemImages` (with `OwnerType = ImageOwnerType.RequestOffer`)
   - When saving: set `OwnerType` and the correct FK

3. `ItemCategory.Semua` removed
   - Controller filter params: use `ItemCategory?` nullable instead
   - Views: remove the Semua option from dropdowns, handle null as "all"

4. `KondisiMinimum` enum removed
   - Views: the "Bekas/Baru/Keduanya" condition selector becomes nullable
   - null = no minimum condition preference

5. `NomorTelepon` removed from `ApplicationUser`
   - Use `user.PhoneNumber` (inherited from IdentityUser) instead
   - Update Register/Edit forms to bind to `PhoneNumber`

6. `Conversation.Type` required on creation
   - Set `Type = ConversationType.Donation` for donation chats
   - Set `Type = ConversationType.RequestOffer` for request offer chats

## Business logic rules to enforce in code

These are not enforced at DB level — enforce them in the relevant controllers:

- `ItemImage`: exactly one of `ItemId`, `ItemRequestId`, `RequestOfferId` should be set, matching `OwnerType`
- `Feedback`: exactly one of `ClaimRequestId`, `RequestOfferId` should be set
- `Conversation`: if `Type = Donation`, set `ItemId` + `ClaimRequestId`; if `Type = RequestOffer`, set `RequestOfferId`
- `PointTransaction`: exactly one of `ClaimRequestId`, `RequestOfferId` should be set
- `Report`: `TargetUserId` and `TargetDonationId` should not both be set
- `Feedback`: `ReviewerId` must not equal `ReviewedUserId`
