import SpriteKit
import SwiftUI

// MARK: - Creature Animation State

enum CreatureAnimationState: Int {
    case idle     = 0
    case interact = 1
    case happy    = 2
    case sad      = 3
}

// MARK: - CreatureSpritesheet
//
// Slices a 4×4 PNG sprite sheet into individual frames.
//
// Sheet layout (matches your SVG design):
//   Columns 0–3 → mutation variants
//   Rows    0–3 → animation states: Idle, Interact, Happy, Sad
//
// PNG naming convention: "{catalogID}_sheet" in Assets.xcassets/Creatures/
// e.g.  Assets.xcassets/Creatures/aquaburst_sheet.imageset/aquaburst_sheet.png
//
// Export your SVGs at 512×512 (128px per frame) or 1024×1024 (256px per frame).

struct CreatureSpritesheet {

    static let columns: CGFloat = 4
    static let rows:    CGFloat = 4

    // MARK: SpriteKit — SKTexture

    /// Returns a texture for one frame (mutation × state).
    /// Falls back to a placeholder color texture if the sheet image is missing.
    static func texture(
        catalogID:      String,
        mutation:       Int,
        state:          CreatureAnimationState
    ) -> SKTexture {
        let sheetName = "creature_\(catalogID)_atlas"

        guard let sheet = SKTexture(imageNamed: sheetName) as SKTexture?,
              !sheetName.isEmpty else {
            return SKTexture(image: placeholderImage(for: catalogID))
        }

        // SpriteKit UV coordinates: origin bottom-left, Y increases upward.
        // Our sheet rows are top-down (row 0 = Idle at top),
        // so we flip the row index.
        let col     = CGFloat(max(0, min(mutation, 3)))
        let row     = CGFloat(3 - max(0, min(state.rawValue, 3)))   // flip Y

        let rect = CGRect(
            x:      col  / columns,
            y:      row  / rows,
            width:  1.0  / columns,
            height: 1.0  / rows
        )

        return SKTexture(rect: rect, in: sheet)
    }

    // MARK: SwiftUI — UIImage crop

    /// Returns a UIImage for one frame, suitable for use in SwiftUI `Image(uiImage:)`.
    /// Used in Shop, Monsterpedia, and other non-animated contexts.
    static func uiImage(
        catalogID: String,
        mutation:  Int,
        state:     CreatureAnimationState = .idle
    ) -> UIImage {
        let sheetName = "creature_\(catalogID)_atlas"

        guard let sheet = UIImage(named: sheetName) else {
            return placeholderImage(for: catalogID)
        }

        let sheetW  = sheet.size.width
        let sheetH  = sheet.size.height
        let frameW  = sheetW / columns
        let frameH  = sheetH / rows

        let col = CGFloat(max(0, min(mutation, 3)))
        let row = CGFloat(max(0, min(state.rawValue, 3)))   // UIKit Y is top-down — no flip needed

        let cropRect = CGRect(
            x:      col  * frameW,
            y:      row  * frameH,
            width:  frameW,
            height: frameH
        )

        guard let cgImage = sheet.cgImage?.cropping(to: cropRect) else {
            return placeholderImage(for: catalogID)
        }

        return UIImage(cgImage: cgImage, scale: sheet.scale, orientation: sheet.imageOrientation)
    }

    // MARK: SwiftUI convenience

    /// Returns a SwiftUI `Image` for use in shop cards, pedia, etc.
    static func image(
        catalogID: String,
        mutation:  Int,
        state:     CreatureAnimationState = .idle
    ) -> Image {
        Image(uiImage: uiImage(catalogID: catalogID, mutation: mutation, state: state))
    }

    // MARK: Placeholder

    /// Coloured rectangle used before real art is added.
    static func placeholderImage(for catalogID: String) -> UIImage {
        let size = CGSize(width: 128, height: 128)
        let renderer = UIGraphicsImageRenderer(size: size)
        return renderer.image { ctx in
            // Deterministic color from catalog ID so each species looks distinct
            let hue = CGFloat(abs(catalogID.hashValue) % 360) / 360.0
            UIColor(hue: hue, saturation: 0.4, brightness: 0.85, alpha: 1).setFill()
            ctx.fill(CGRect(origin: .zero, size: size))
        }
    }
}

// MARK: - CreatureSpriteNode
//
// A SpriteKit node that displays a creature and can animate between states.
// Drop this into your HabitatScene when you build the SpriteKit habitat view.

class CreatureSpriteNode: SKSpriteNode {

    let catalogID:     String
    var mutationIndex: Int
    private(set) var currentState: CreatureAnimationState = .idle

    init(catalogID: String, mutationIndex: Int, size: CGSize = CGSize(width: 128, height: 128)) {
        self.catalogID     = catalogID
        self.mutationIndex = mutationIndex
        let initialTexture = CreatureSpritesheet.texture(
            catalogID: catalogID,
            mutation:  mutationIndex,
            state:     .idle
        )
        super.init(texture: initialTexture, color: .clear, size: size)
    }

    required init?(coder aDecoder: NSCoder) { fatalError("Use init(catalogID:mutationIndex:size:)") }

    /// Transition to a new animation state with a brief cross-fade.
    func transition(to state: CreatureAnimationState, duration: TimeInterval = 0.15) {
        guard state != currentState else { return }
        currentState = state
        let newTexture = CreatureSpritesheet.texture(
            catalogID: catalogID,
            mutation:  mutationIndex,
            state:     state
        )
        run(.setTexture(newTexture, resize: false))
    }

    /// Play the interact animation then return to idle.
    func playInteract() {
        transition(to: .interact)
        run(.sequence([
            .wait(forDuration: 0.8),
            .run { [weak self] in self?.transition(to: .idle) }
        ]))
    }

    /// Reflect the creature's current happiness stats visually.
    func updateFromStats(happiness: Double) {
        switch happiness {
        case 0.6...: transition(to: .happy)
        case 0.3..<0.6: transition(to: .idle)
        default:     transition(to: .sad)
        }
    }
}

// MARK: - SwiftUI Creature Image View
//
// Drop-in replacement for the emoji placeholders in ShopView, HabitatView, PediaView.
// Automatically uses real art when available, falls back to placeholder color tile.

struct CreatureImageView: View {
    let catalogID:  String
    let mutation:   Int
    let state:      CreatureAnimationState
    let size:       CGFloat

    init(
        catalogID: String,
        mutation:  Int  = 0,
        state:     CreatureAnimationState = .idle,
        size:      CGFloat = 80
    ) {
        self.catalogID = catalogID
        self.mutation  = mutation
        self.state     = state
        self.size      = size
    }

    var body: some View {
        Image(uiImage: CreatureSpritesheet.uiImage(
            catalogID: catalogID,
            mutation:  mutation,
            state:     state
        ))
        .resizable()
        .interpolation(.none)      // Keep pixel-art sharp; remove for smooth art
        .aspectRatio(1, contentMode: .fit)
        .frame(width: size, height: size)
    }
}

// MARK: - Preview

#Preview {
    VStack(spacing: 20) {
        Text("Real art (when PNG is in Assets):").font(.caption)
        HStack(spacing: 12) {
            ForEach(0..<4) { mutation in
                CreatureImageView(catalogID: "aquaburst", mutation: mutation, size: 64)
            }
        }
        Text("Placeholder (no PNG yet):").font(.caption)
        HStack(spacing: 12) {
            ForEach(0..<4) { mutation in
                CreatureImageView(catalogID: "mystery_creature", mutation: mutation, size: 64)
            }
        }
    }
    .padding()
}
