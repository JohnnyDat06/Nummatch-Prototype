# NumMatch Casual Puzzle

![Unity Version](https://img.shields.io/badge/Unity-2021.3_LTS-lightgrey.svg)
![Language](https://img.shields.io/badge/Language-C%23-blue.svg)
![Platform](https://img.shields.io/badge/Platform-Windows_Standalone-brightgreen.svg)

NumMatch là một tựa game giải đố (casual puzzle) được phát triển trên **Unity 2021.3 LTS** dưới dạng bài test kỹ năng (Pentoro Intern Test R2). Dự án tuân thủ nghiêm ngặt các nguyên tắc OOP, SOLID và được tổ chức thư mục chặt chẽ, tách bạch rõ ràng giữa Core Logic và UI.

## 🌟 Tính năng chính

### 1. Easy Mode - Gem Collection
- **Luật chơi cốt lõi:** Người chơi có thể nối 2 ô số nếu chúng **giống nhau** hoặc có **tổng bằng 10** và không bị chặn bởi các ô số khác (xét theo 4 hướng: ngang, dọc, chéo).
- **Hệ thống màn chơi (Stages):** Bàn chơi tự động được khởi tạo thuật toán theo từng Stage (thuật toán phân phối số ngẫu nhiên nhưng đảm bảo giới hạn cặp Match).
- **Tính năng Add Number:** Khi hết nước đi, người chơi có thể nhấn nút "Add" để sao chép các ô chưa ghép và nối thêm vào cuối bàn cờ.
- **Tính năng thu thập Gem:** Gem xuất hiện ngẫu nhiên với tỷ lệ tinh chỉnh mỗi khi có ô mới được sinh ra. Trò chơi kết thúc khi thu thập đủ số lượng Gem yêu cầu hoặc kiệt lượt Add.

### 2. Hard Mode Solver (Bonus)
Một công cụ (Editor Tool) được thiết kế riêng để giải quyết thuật toán Hard Mode với độ phức tạp cao, không cần vào Play Mode.
- **Thuật toán:** Sử dụng **IDDFS** kết hợp với **Memoization** (Transposition Table) và kỹ thuật biểu diễn trạng thái bằng **Bitmask (128-bit)**.
- **Tối ưu cực độ (O(N)):** Thay thế thuật toán kiểm tra đường đi bằng kỹ thuật **Raycasting 4 hướng**, giảm thời gian duyệt từ O(N³) xuống O(N).
- **Tốc độ (Benchmark):** Xử lý input chuỗi 99 ký tự để xuất ra top 10 lời giải tối ưu nhất với giới hạn thời gian (Timeout) chưa tới 1000ms.
- **File đặc tả thuật toán:** HardModeAlgorithm.md

## 🛠 Tech Stack & Kiến trúc

- **Engine:** Unity 2021.3 LTS
- **UI:** uGUI + TextMeshPro (Canvas Scaler Fixed Resolution: 1080x1920 Portrait)
- **Kiến trúc:**
  - `Core/`: Chứa các Model (`Cell`, `BoardData`) và Logic hệ thống (`MatchValidator`, `BoardGenerator`, `GemSpawner`). Tách biệt hoàn toàn khỏi `UnityEngine` (Ngoại trừ Debug).
  - `Managers/`: Quản lý luồng Game (`BoardManager`, `GameStateManager`, `AudioManager`, `SceneController`).
  - `UI/`: Tách biệt logic giao diện (Component hiển thị View, Popups).
  - Cấm hoàn toàn việc lạm dụng `FindObjectOfType` trong `Update`, hardcode magic numbers và sử dụng Mảng 2D (`Cell[,]`). Dự án sử dụng cấu trúc Mảng 1D tính toán vị trí linh hoạt.

## 🎮 Cách chơi
1. **Match:** Chạm vào 2 ô có cùng giá trị (VD: `3` và `3`) hoặc tổng bằng 10 (VD: `4` và `6`). Hai ô này phải có đường nối thông nhau không bị che khuất (Ngang, dọc, hoặc chéo).
2. **Clear Row:** Nếu một hàng ngang đã được ghép hết, nó sẽ tự động bị xóa và các hàng dưới sẽ trượt lên.
3. **Thu thập Gem:** Ưu tiên nối các ô số mang biểu tượng viên ngọc (Gem) để tích lũy. Khi thanh Gem đạt mốc yêu cầu, bạn sẽ chiến thắng.
4. **Add:** Khi bị bí, nhấn vào nút Add (+) để chép lại số xuống cuối bảng cờ và mở ra cơ hội nối mới. Nếu nút Add về 0 và hết đường đi, bạn sẽ thua (Lose).

## 🚀 Hướng dẫn cài đặt

1. Clone Repository này về máy của bạn:
   ```bash
   git clone https://github.com/your-username/NumMatch-Prototype.git
   ```
2. Mở Unity Hub, chọn **Add Project** và trỏ tới thư mục vừa clone.
3. Chắc chắn rằng bạn đang sử dụng **Unity 2021.3 LTS** để tránh các lỗi liên quan đến phiên bản.
4. Vào thư mục `Assets/Scenes/`, mở Scene `Home` và nhấn nút Play trên Editor để trải nghiệm.
5. Để test công cụ **Hard Mode Solver**, trên thanh Menu bar của Unity, chọn `NumMatch` -> `Run Hard Mode Solver`. Đọc kết quả trong Console hoặc file `output.txt` tại `Assets/StreamingAssets`.

## 📂 Cấu trúc thư mục (Tóm tắt)
```text
Assets/Scripts/
├── Core/       # (Data classes, Generator, Spawner - No Monobehaviour)
├── Managers/   # (Board, GameState, Audio, UI, Scene - Monobehaviour Singletons/Managers)
├── UI/         # (Views, Popups)
├── Utils/      # (Helper classes)
├── Editor/     # (HardModeSolver, Tests)
└── Tests/      # (Unit tests via NUnit)
```

## 📜 Tài liệu mở rộng
Bạn có thể tham khảo luồng thiết kế thuật toán chi tiết của phần Hard Mode Solver tại: `Docs/HardModeAlgorithm.md`

---
*Dự án thực hiện cho bài test vòng 2 của Pentoro Intern - Tác giả: JohnnyDat06*
