import Foundation

// MARK: - Player Economy State

struct PlayerEconomy: Codable {
    var coins: Int
    var stardust: Int

    // Lifetime stats for analytics / achievement tracking
    var totalCoinsEarned: Int
    var totalCoinsSpent: Int
    var totalStardustSpent: Int

    init(startingCoins: Int = 500, startingStardust: Int = 0) {
        self.coins              = startingCoins
        self.stardust           = startingStardust
        self.totalCoinsEarned   = startingCoins
        self.totalCoinsSpent    = 0
        self.totalStardustSpent = 0
    }

    // MARK: Transactions

    /// Returns true if the player can afford the given coin cost.
    func canAffordCoins(_ amount: Int) -> Bool {
        return coins >= amount
    }

    func canAffordStardust(_ amount: Int) -> Bool {
        return stardust >= amount
    }

    /// Spend coins. Returns false if insufficient funds.
    @discardableResult
    mutating func spendCoins(_ amount: Int) -> Bool {
        guard canAffordCoins(amount) else { return false }
        coins         -= amount
        totalCoinsSpent += amount
        return true
    }

    @discardableResult
    mutating func spendStardust(_ amount: Int) -> Bool {
        guard canAffordStardust(amount) else { return false }
        stardust          -= amount
        totalStardustSpent += amount
        return true
    }

    mutating func earnCoins(_ amount: Int) {
        coins            += amount
        totalCoinsEarned += amount
    }

    mutating func earnStardust(_ amount: Int) {
        stardust += amount
    }
}

// MARK: - Pricing Tables

/// Sourced from economy_system_v1.json
struct PricingTable {

    // MARK: Food
    struct Food {
        let id: String
        let name: String
        let coinCost: Int
        let hungerRestored: Double  // 0.0–1.0

        static let catalog: [Food] = [
            Food(id: "basic_kibble",   name: "Basic Kibble",   coinCost: 10, hungerRestored: 0.25),
            Food(id: "hearty_stew",    name: "Hearty Stew",    coinCost: 20, hungerRestored: 0.50),
            Food(id: "magical_feast",  name: "Magical Feast",  coinCost: 30, hungerRestored: 1.00),
        ]
    }

    // MARK: Breeding
    struct Breeding {
        /// Cost to breed two creatures of matching rarity
        static func cost(rarity: Rarity) -> Int {
            switch rarity {
            case .common:   return 300
            case .uncommon: return 800
            case .rare:     return 2500
            }
        }
    }

    // MARK: Customer Orders
    struct CustomerOrder {
        let id: UUID
        let requiredCreatureCatalogID: String
        let requiredRarity: Rarity
        let minHappiness: Double    // 0.0–1.0 threshold
        let coinReward: Int
        let expiresAt: Date
        var isFulfilled: Bool

        init(
            creatureCatalogID: String,
            rarity: Rarity,
            minHappiness: Double,
            coinReward: Int,
            expiresInHours: Double = 8
        ) {
            self.id                      = UUID()
            self.requiredCreatureCatalogID = creatureCatalogID
            self.requiredRarity          = rarity
            self.minHappiness            = minHappiness
            self.coinReward              = coinReward
            self.expiresAt               = Date().addingTimeInterval(expiresInHours * 3600)
            self.isFulfilled             = false
        }

        var isExpired: Bool { Date() >= expiresAt }

        /// Base reward range by rarity. Actual reward set at order creation.
        static func rewardRange(rarity: Rarity) -> ClosedRange<Int> {
            switch rarity {
            case .common:   return 50...200
            case .uncommon: return 200...500
            case .rare:     return 500...1500
            }
        }
    }

    // MARK: Sell Values
    /// Calculate how many coins a player gets for selling a creature.
    static func sellValue(rarity: Rarity, happinessMultiplier: Double) -> Int {
        let base = rarity.baseSellValue
        let multiplier = max(1.0, min(2.0, happinessMultiplier))
        return Int(Double(base) * multiplier)
    }
}

// MARK: - IAP / Stardust Packages

struct StardustPackage: Identifiable {
    let id: String
    let stardust: Int
    let usdPrice: Double
    let displayPrice: String    // e.g. "$4.99"

    static let packages: [StardustPackage] = [
        StardustPackage(id: "stardust_500",  stardust: 500,  usdPrice: 4.99,  displayPrice: "$4.99"),
        StardustPackage(id: "stardust_1000", stardust: 1000, usdPrice: 9.99,  displayPrice: "$9.99"),
        StardustPackage(id: "stardust_2500", stardust: 2500, usdPrice: 19.99, displayPrice: "$19.99"),
    ]
}
