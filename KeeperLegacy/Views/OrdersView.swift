import SwiftUI

// MARK: - Orders View
// Displays active customer orders. Player fulfills orders by providing matching creatures.
// Sourced from economy_system_v1.json: 2-4 orders active, refresh every 6-8 hrs.

struct OrdersView: View {
    @EnvironmentObject var orderVM:    CustomerOrderViewModel
    @EnvironmentObject var progressVM: ProgressionViewModel

    var body: some View {
        NavigationStack {
            VStack(spacing: 0) {
                earningsHeader
                orderList
            }
            .navigationTitle("Customer Orders")
            .navigationBarTitleDisplayMode(.large)
            .background(Color(hex: "#FFF8F0"))
            .alert(item: $orderVM.lastFulfillmentResult) { result in
                switch result {
                case .success(let name, let coins):
                    return Alert(
                        title: Text("Order Fulfilled!"),
                        message: Text("\(name) found a happy home. You earned \(coins) coins!"),
                        dismissButton: .default(Text("Great!"))
                    )
                case .failure(let reason):
                    return Alert(
                        title: Text("Can't Fulfill"),
                        message: Text(reason),
                        dismissButton: .default(Text("OK"))
                    )
                }
            }
        }
    }

    // MARK: Earnings Summary Header

    private var earningsHeader: some View {
        HStack(spacing: 16) {
            VStack(alignment: .leading, spacing: 4) {
                Text("Active Orders")
                    .font(.system(size: 13, weight: .medium, design: .rounded))
                    .foregroundColor(.secondary)
                Text("\(orderVM.activeOrders.count)")
                    .font(.system(size: 28, weight: .bold, design: .rounded))
            }
            Spacer()
            VStack(alignment: .trailing, spacing: 4) {
                Text("Potential Earnings")
                    .font(.system(size: 13, weight: .medium, design: .rounded))
                    .foregroundColor(.secondary)
                HStack(spacing: 4) {
                    Image(systemName: "circle.fill")
                        .foregroundColor(Color(hex: "#FFD700"))
                        .font(.system(size: 12))
                    Text("\(totalPotentialEarnings)")
                        .font(.system(size: 22, weight: .bold, design: .rounded))
                }
            }
        }
        .padding(.horizontal, 20)
        .padding(.vertical, 14)
        .background(.white.opacity(0.8))
    }

    private var totalPotentialEarnings: Int {
        orderVM.activeOrders.reduce(0) { $0 + Int($1.coinReward) }
    }

    // MARK: Order List

    @ViewBuilder
    private var orderList: some View {
        if orderVM.activeOrders.isEmpty {
            emptyOrdersView
        } else {
            ScrollView {
                VStack(spacing: 14) {
                    // How-to hint for new players
                    HowToFulfillBanner()

                    ForEach(orderVM.activeOrders, id: \.id) { order in
                        OrderCard(order: order)
                            .environmentObject(orderVM)
                            .environmentObject(progressVM)
                    }
                }
                .padding(16)
            }
        }
    }

    private var emptyOrdersView: some View {
        VStack(spacing: 20) {
            Spacer()
            Image(systemName: "bag")
                .font(.system(size: 60))
                .foregroundColor(Color(hex: "#FFB347").opacity(0.4))
            Text("No orders right now")
                .font(.system(size: 18, weight: .bold, design: .rounded))
            Text("New customers visit every few hours.\nCheck back soon!")
                .font(.system(size: 15))
                .foregroundColor(.secondary)
                .multilineTextAlignment(.center)
            Spacer()
        }
        .padding()
    }
}

// MARK: - How-To Banner (shown until first fulfillment)

struct HowToFulfillBanner: View {
    @AppStorage("hasSeenOrdersHint") private var hasSeenHint = false

    var body: some View {
        if !hasSeenHint {
            HStack(spacing: 12) {
                Image(systemName: "lightbulb.fill")
                    .foregroundColor(Color(hex: "#FFD700"))
                    .font(.system(size: 20))
                VStack(alignment: .leading, spacing: 3) {
                    Text("How orders work")
                        .font(.system(size: 13, weight: .bold, design: .rounded))
                    Text("Customers want specific creatures. Buy or breed the right one, keep it happy, then tap Fulfill.")
                        .font(.system(size: 12))
                        .foregroundColor(.secondary)
                }
                Spacer()
                Button {
                    hasSeenHint = true
                } label: {
                    Image(systemName: "xmark")
                        .font(.system(size: 12))
                        .foregroundColor(.secondary)
                }
            }
            .padding(14)
            .background(Color(hex: "#FFD700").opacity(0.1))
            .clipShape(RoundedRectangle(cornerRadius: 14))
            .overlay(
                RoundedRectangle(cornerRadius: 14)
                    .stroke(Color(hex: "#FFD700").opacity(0.3), lineWidth: 1)
            )
        }
    }
}

// MARK: - Order Card

struct OrderCard: View {
    @EnvironmentObject var orderVM:    CustomerOrderViewModel
    @EnvironmentObject var progressVM: ProgressionViewModel

    let order: CustomerOrderEntity

    @State private var showFulfillConfirm = false

    private var catalogEntry: CreatureCatalogEntry? {
        CreatureCatalogEntry.find(byID: order.requiredCatalogID ?? "")
    }

    private var canFulfill: Bool {
        orderVM.canFulfill(order)
    }

    private var fulfillCreature: CreatureEntity? {
        orderVM.fulfillingCreature(for: order)
    }

    var body: some View {
        VStack(spacing: 0) {
            // Top: creature info + reward
            HStack(spacing: 14) {
                creaturePortrait
                orderDetails
                Spacer()
                rewardBadge
            }
            .padding(16)

            Divider().padding(.horizontal, 16)

            // Bottom: status + action
            HStack(spacing: 12) {
                // Timer
                HStack(spacing: 5) {
                    Image(systemName: "clock")
                        .font(.system(size: 11))
                        .foregroundColor(Color(hex: orderVM.urgencyColor(for: order)))
                    Text(orderVM.timeRemaining(for: order))
                        .font(.system(size: 12, weight: .semibold, design: .rounded))
                        .foregroundColor(Color(hex: orderVM.urgencyColor(for: order)))
                }

                Spacer()

                // Status indicator
                if canFulfill {
                    HStack(spacing: 5) {
                        Image(systemName: "checkmark.circle.fill")
                            .foregroundColor(.green)
                            .font(.system(size: 13))
                        Text("Ready to fulfill")
                            .font(.system(size: 12, weight: .medium, design: .rounded))
                            .foregroundColor(.green)
                    }
                } else {
                    HStack(spacing: 5) {
                        Image(systemName: "xmark.circle")
                            .foregroundColor(.secondary)
                            .font(.system(size: 13))
                        Text("Need this creature")
                            .font(.system(size: 12))
                            .foregroundColor(.secondary)
                    }
                }

                // Fulfill button
                Button {
                    showFulfillConfirm = true
                } label: {
                    Text("Fulfill")
                        .font(.system(size: 13, weight: .bold, design: .rounded))
                        .foregroundColor(.white)
                        .padding(.horizontal, 16)
                        .padding(.vertical, 8)
                        .background(canFulfill ? Color(hex: "#A8D5A8") : Color.gray.opacity(0.3))
                        .clipShape(Capsule())
                }
                .disabled(!canFulfill)
            }
            .padding(.horizontal, 16)
            .padding(.vertical, 10)
        }
        .background(.white)
        .clipShape(RoundedRectangle(cornerRadius: 16))
        .shadow(color: .black.opacity(0.06), radius: 6, x: 0, y: 3)
        .confirmationDialog(fulfillDialogTitle, isPresented: $showFulfillConfirm, titleVisibility: .visible) {
            Button("Fulfill Order (+\(order.coinReward) coins)", role: .none) {
                orderVM.fulfill(order: order, progressVM: progressVM)
            }
            Button("Cancel", role: .cancel) { }
        } message: {
            Text(fulfillDialogMessage)
        }
    }

    // MARK: Subviews

    private var creaturePortrait: some View {
        ZStack {
            RoundedRectangle(cornerRadius: 12)
                .fill(
                    (catalogEntry.map { Color(hex: $0.habitatType.displayColor) } ?? Color.gray)
                        .opacity(0.3)
                )
                .frame(width: 64, height: 64)

            Text(habitatEmoji)
                .font(.system(size: 34))
        }
    }

    private var orderDetails: some View {
        VStack(alignment: .leading, spacing: 5) {
            Text(catalogEntry?.name ?? "Unknown Creature")
                .font(.system(size: 15, weight: .bold, design: .rounded))
                .lineLimit(1)

            HStack(spacing: 6) {
                if let entry = catalogEntry {
                    RarityBadge(rarity: entry.rarity)
                    Text(entry.habitatType.rawValue)
                        .font(.system(size: 11, design: .rounded))
                        .foregroundColor(.secondary)
                }
            }

            HStack(spacing: 4) {
                Image(systemName: "face.smiling")
                    .font(.system(size: 10))
                    .foregroundColor(.secondary)
                Text("Min \(Int(order.minHappiness * 100))% happiness")
                    .font(.system(size: 11))
                    .foregroundColor(.secondary)
            }
        }
    }

    private var rewardBadge: some View {
        VStack(spacing: 3) {
            Image(systemName: "circle.fill")
                .foregroundColor(Color(hex: "#FFD700"))
                .font(.system(size: 14))
            Text("\(order.coinReward)")
                .font(.system(size: 16, weight: .bold, design: .rounded))
            Text("coins")
                .font(.system(size: 10, design: .rounded))
                .foregroundColor(.secondary)
        }
    }

    // MARK: Dialog Text

    private var fulfillDialogTitle: String {
        if let creature = fulfillCreature,
           let catalogID = creature.catalogID,
           let entry = CreatureCatalogEntry.find(byID: catalogID) {
            return "Send \(entry.name) to their new home?"
        }
        return "Fulfill this order?"
    }

    private var fulfillDialogMessage: String {
        "The creature will leave your shop. You'll earn \(order.coinReward) coins."
    }

    private var habitatEmoji: String {
        switch catalogEntry?.habitatType {
        case .water:    return "🌊"
        case .dirt:     return "🪨"
        case .grass:    return "🌿"
        case .fire:     return "🔥"
        case .ice:      return "❄️"
        case .electric: return "⚡"
        case .magical:  return "✨"
        default:        return "❓"
        }
    }
}

// MARK: - Preview

#Preview {
    OrdersView()
        .environmentObject(CustomerOrderViewModel())
        .environmentObject(ProgressionViewModel())
}
