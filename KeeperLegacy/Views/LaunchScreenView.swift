import SwiftUI

// MARK: - Launch Screen
// Displayed while the app initializes Core Data and loads game state.
// Pastel gradient + logo + sparkle particles per art_assets_v1.json spec.

struct LaunchScreenView: View {

    @State private var sparkles: [SparkleParticle] = SparkleParticle.generate(count: 12)
    @State private var opacity: Double = 0
    @State private var scale: Double  = 0.85

    var body: some View {
        ZStack {
            // Background gradient: sky blue → magical purple
            LinearGradient(
                colors: [Color(hex: "#A8D8EA"), Color(hex: "#C99BFF")],
                startPoint: .topLeading,
                endPoint: .bottomTrailing
            )
            .ignoresSafeArea()

            // Sparkle particles
            ForEach(sparkles) { sparkle in
                SparkleView(particle: sparkle)
            }

            // Logo
            VStack(spacing: 16) {
                Image(systemName: "sparkles")
                    .font(.system(size: 64, weight: .light))
                    .foregroundStyle(
                        LinearGradient(
                            colors: [Color(hex: "#FFD700"), Color(hex: "#7FFFD4")],
                            startPoint: .topLeading,
                            endPoint: .bottomTrailing
                        )
                    )
                    .shadow(color: .white.opacity(0.6), radius: 12)

                Text("Keeper's Legacy")
                    .font(.system(size: 36, weight: .bold, design: .rounded))
                    .foregroundColor(.white)
                    .shadow(color: Color(hex: "#6A5ACD").opacity(0.5), radius: 6, x: 0, y: 3)

                Text("A magical creature shop awaits")
                    .font(.system(size: 16, weight: .medium, design: .rounded))
                    .foregroundColor(.white.opacity(0.85))
            }
            .scaleEffect(scale)
            .opacity(opacity)
        }
        .onAppear {
            withAnimation(.easeOut(duration: 0.8)) {
                opacity = 1
                scale   = 1
            }
            animateSparkles()
        }
    }

    private func animateSparkles() {
        withAnimation(.easeInOut(duration: 2.0).repeatForever(autoreverses: true)) {
            sparkles = sparkles.map { var s = $0; s.opacity = Double.random(in: 0.2...1.0); return s }
        }
    }
}

// MARK: - Sparkle Particle

struct SparkleParticle: Identifiable {
    let id = UUID()
    var x: CGFloat
    var y: CGFloat
    var size: CGFloat
    var opacity: Double
    var rotation: Double

    static func generate(count: Int) -> [SparkleParticle] {
        (0..<count).map { _ in
            SparkleParticle(
                x: CGFloat.random(in: 0.05...0.95),
                y: CGFloat.random(in: 0.05...0.95),
                size: CGFloat.random(in: 8...20),
                opacity: Double.random(in: 0.4...1.0),
                rotation: Double.random(in: 0...360)
            )
        }
    }
}

struct SparkleView: View {
    let particle: SparkleParticle

    var body: some View {
        GeometryReader { geo in
            Image(systemName: "sparkle")
                .font(.system(size: particle.size))
                .foregroundColor(.white.opacity(particle.opacity))
                .rotationEffect(.degrees(particle.rotation))
                .position(
                    x: particle.x * geo.size.width,
                    y: particle.y * geo.size.height
                )
        }
    }
}

// MARK: - Color Hex Extension

extension Color {
    init(hex: String) {
        let hex = hex.trimmingCharacters(in: CharacterSet.alphanumerics.inverted)
        var int: UInt64 = 0
        Scanner(string: hex).scanHexInt64(&int)
        let r, g, b: Double
        switch hex.count {
        case 6:
            r = Double((int >> 16) & 0xFF) / 255
            g = Double((int >>  8) & 0xFF) / 255
            b = Double( int        & 0xFF) / 255
        default:
            r = 1; g = 1; b = 1
        }
        self.init(red: r, green: g, blue: b)
    }
}

// MARK: - Preview

#Preview {
    LaunchScreenView()
}
