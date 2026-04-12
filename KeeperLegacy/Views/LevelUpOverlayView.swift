import SwiftUI

// MARK: - Level-Up Overlay
// Shown on top of all content when the player gains a level.
// Dismissed automatically after 3 seconds or on tap.

struct LevelUpOverlayView: View {
    let event: LevelUpEvent
    let onDismiss: () -> Void

    @State private var scale:   CGFloat = 0.5
    @State private var opacity: Double  = 0
    @State private var sparkles = SparkleParticle.generate(count: 16)

    var body: some View {
        ZStack {
            // Dim background
            Color.black.opacity(0.45)
                .ignoresSafeArea()
                .onTapGesture { dismiss() }

            // Sparkle layer
            ForEach(sparkles) { s in
                SparkleView(particle: s)
            }

            // Card
            VStack(spacing: 20) {
                // Level badge
                ZStack {
                    Circle()
                        .fill(
                            LinearGradient(
                                colors: [Color(hex: "#C99BFF"), Color(hex: "#7FFFD4")],
                                startPoint: .topLeading,
                                endPoint: .bottomTrailing
                            )
                        )
                        .frame(width: 100, height: 100)
                        .shadow(color: Color(hex: "#C99BFF").opacity(0.5), radius: 20)

                    VStack(spacing: 0) {
                        Text("LV")
                            .font(.system(size: 14, weight: .black, design: .rounded))
                            .foregroundColor(.white.opacity(0.85))
                        Text("\(event.newLevel)")
                            .font(.system(size: 38, weight: .black, design: .rounded))
                            .foregroundColor(.white)
                    }
                }

                Text("Level Up!")
                    .font(.system(size: 30, weight: .black, design: .rounded))
                    .foregroundColor(.white)

                // XP milestone reward
                if let milestone = event.milestoneReward {
                    MilestoneHighlightCard(milestone: milestone)
                }

                // Feature unlock
                if let feature = event.unlockedFeature {
                    FeatureUnlockBadge(feature: feature)
                }

                // Tap to continue
                Text("Tap anywhere to continue")
                    .font(.system(size: 13))
                    .foregroundColor(.white.opacity(0.6))
                    .padding(.top, 4)
            }
            .padding(32)
            .background(.ultraThinMaterial, in: RoundedRectangle(cornerRadius: 28))
            .padding(.horizontal, 36)
            .scaleEffect(scale)
            .opacity(opacity)
        }
        .onAppear {
            withAnimation(.spring(response: 0.4, dampingFraction: 0.65)) {
                scale   = 1
                opacity = 1
            }
            // Auto-dismiss after 4 seconds if not tapped
            DispatchQueue.main.asyncAfter(deadline: .now() + 4) {
                dismiss()
            }
        }
    }

    private func dismiss() {
        withAnimation(.easeIn(duration: 0.25)) {
            opacity = 0
            scale   = 0.9
        }
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.25) {
            onDismiss()
        }
    }
}

// MARK: - Milestone Highlight Card

struct MilestoneHighlightCard: View {
    let milestone: MilestoneReward

    var body: some View {
        VStack(spacing: 8) {
            Text("Milestone Reward")
                .font(.system(size: 11, weight: .semibold, design: .rounded))
                .foregroundColor(.secondary)
                .textCase(.uppercase)
                .tracking(1)

            HStack(spacing: 16) {
                if milestone.coinBonus > 0 {
                    HStack(spacing: 5) {
                        Image(systemName: "circle.fill")
                            .foregroundColor(Color(hex: "#FFD700"))
                        Text("+\(milestone.coinBonus)")
                            .font(.system(size: 16, weight: .bold, design: .rounded))
                    }
                }
                if milestone.stardustBonus > 0 {
                    HStack(spacing: 5) {
                        Image(systemName: "sparkles")
                            .foregroundColor(Color(hex: "#C99BFF"))
                        Text("+\(milestone.stardustBonus)")
                            .font(.system(size: 16, weight: .bold, design: .rounded))
                    }
                }
            }

            if let story = milestone.storyEvent {
                Text("New story chapter unlocked")
                    .font(.system(size: 12))
                    .foregroundColor(Color(hex: "#C99BFF"))
                    .italic()
            }
        }
        .padding(14)
        .background(Color(hex: "#FFD700").opacity(0.12))
        .clipShape(RoundedRectangle(cornerRadius: 12))
    }
}

// MARK: - Feature Unlock Badge

struct FeatureUnlockBadge: View {
    let feature: GameFeature

    var body: some View {
        HStack(spacing: 10) {
            Image(systemName: "lock.open.fill")
                .foregroundColor(Color(hex: "#7FFFD4"))
                .font(.system(size: 18))
            VStack(alignment: .leading, spacing: 2) {
                Text("New Feature Unlocked")
                    .font(.system(size: 11, weight: .semibold, design: .rounded))
                    .foregroundColor(.secondary)
                    .textCase(.uppercase)
                    .tracking(0.5)
                Text(featureDisplayName)
                    .font(.system(size: 15, weight: .bold, design: .rounded))
            }
        }
        .padding(14)
        .background(Color(hex: "#7FFFD4").opacity(0.12))
        .clipShape(RoundedRectangle(cornerRadius: 12))
    }

    private var featureDisplayName: String {
        switch feature {
        case .breeding:         return "Breeding"
        case .mutations:        return "Mutation Variants"
        case .monsterpedia:     return "Monsterpedia"
        case .habitatExpansion: return "Habitat Expansion"
        case .cosmetics:        return "Cosmetics Shop"
        case .magicalHabitat:   return "Magical Habitat"
        case .customerOrders:   return "Customer Orders"
        default:                return feature.rawValue.capitalized
        }
    }
}

// MARK: - Milestone Story Sheet
// Shown separately from the level-up overlay for story-specific milestone reveals.

struct MilestoneStorySheet: View {
    let milestone: MilestoneReward
    @Environment(\.dismiss) private var dismiss

    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(spacing: 28) {
                    // Header
                    ZStack {
                        RoundedRectangle(cornerRadius: 20)
                            .fill(
                                LinearGradient(
                                    colors: [Color(hex: "#C99BFF").opacity(0.3), Color(hex: "#7FFFD4").opacity(0.3)],
                                    startPoint: .topLeading,
                                    endPoint: .bottomTrailing
                                )
                            )
                            .frame(height: 160)
                        VStack(spacing: 8) {
                            Image(systemName: "scroll.fill")
                                .font(.system(size: 44))
                                .foregroundColor(Color(hex: "#C99BFF"))
                            Text("Level \(milestone.id) Milestone")
                                .font(.system(size: 14, weight: .semibold, design: .rounded))
                                .foregroundColor(.secondary)
                        }
                    }
                    .padding(.horizontal)

                    // Milestone description
                    VStack(alignment: .leading, spacing: 12) {
                        Text(milestone.description)
                            .font(.system(size: 17, weight: .bold, design: .rounded))

                        if milestone.coinBonus > 0 || milestone.stardustBonus > 0 {
                            Divider()
                            HStack(spacing: 20) {
                                if milestone.coinBonus > 0 {
                                    HStack(spacing: 6) {
                                        Image(systemName: "circle.fill")
                                            .foregroundColor(Color(hex: "#FFD700"))
                                        Text("+\(milestone.coinBonus) Coins")
                                            .font(.system(size: 15, weight: .semibold, design: .rounded))
                                    }
                                }
                                if milestone.stardustBonus > 0 {
                                    HStack(spacing: 6) {
                                        Image(systemName: "sparkles")
                                            .foregroundColor(Color(hex: "#C99BFF"))
                                        Text("+\(milestone.stardustBonus) Stardust")
                                            .font(.system(size: 15, weight: .semibold, design: .rounded))
                                    }
                                }
                            }
                        }

                        if let feature = milestone.unlocksFeature {
                            Divider()
                            FeatureUnlockBadge(feature: feature)
                        }
                    }
                    .padding(.horizontal, 20)
                }
                .padding(.vertical, 20)
            }
            .navigationTitle("Milestone")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .confirmationAction) {
                    Button("Continue") { dismiss() }
                        .fontWeight(.bold)
                }
            }
        }
    }
}

// MARK: - Preview

#Preview("Level Up — with milestone") {
    LevelUpOverlayView(
        event: LevelUpEvent(
            newLevel: 10,
            unlockedFeature: nil,
            milestoneReward: MilestoneReward.milestones[10]
        ),
        onDismiss: {}
    )
}

#Preview("Level Up — feature unlock") {
    LevelUpOverlayView(
        event: LevelUpEvent(
            newLevel: 12,
            unlockedFeature: .breeding,
            milestoneReward: MilestoneReward.milestones[12]
        ),
        onDismiss: {}
    )
}
