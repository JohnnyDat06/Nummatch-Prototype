# Hard Mode Algorithm & Solver

Tài liệu này mô tả chi tiết thuật toán giải bài toán Hard Mode của NumMatch.

## 1. Tổng quan bài toán
- **Input:** Chuỗi tối đa 99 ký tự số (1-9) biểu diễn board. Đọc từ `Assets/StreamingAssets/input.txt`.
- **Output:** 10 lời giải (hoặc ít hơn nếu không đủ), mỗi lời giải là một chuỗi các bước di chuyển có định dạng `r1,c1,r2,c2|...`. Lưu tại `Assets/StreamingAssets/output.txt`.
- **Rule Hard Mode:**
  - Không xóa hàng khi match hết hàng (khác Easy Mode).
  - Số `5` đóng vai trò là Gem. Cần thu thập `targetGems`.
  - Mục tiêu thu thập: Nếu có `N` số 5, thu thập `(N/2)*2` nếu lẻ, `N` nếu chẵn.
  - Mỗi lượt match tốn 1 move.
- **Mục tiêu tối ưu:** Tìm lời giải dùng ít số move nhất có thể, thời gian chạy dưới 1000ms.

## 2. Biểu diễn trạng thái (Bitmask)
- Bảng game có tối đa 99 ô. Một kiểu nguyên `ulong` có 64 bit, vì vậy chúng ta sử dụng `struct State` gồm 2 biến `mask0` và `mask1`:
  - `mask0`: Quản lý 64 ô đầu tiên (từ index 0 đến 63).
  - `mask1`: Quản lý các ô còn lại (từ index 64 đến 127).
- Tại mỗi bit:
  - `1`: Ô đã bị matched (đã xóa).
  - `0`: Ô chưa bị matched.
- Cách tính index: `index = row * cols + col` với `cols = 9`.
- **Lợi ích:** 
  - Hash nhanh (sử dụng toán tử XOR trên 2 biến `mask`) và so sánh trạng thái O(1).
  - Copy mảng `State` qua biến tham trị (struct) siêu nhanh thay vì clone Array/List.

## 3. Thuật toán IDDFS (Iterative Deepening Depth First Search)
- **IDDFS** kết hợp được ưu điểm của BFS (luôn tìm thấy đường đi ngắn nhất) và DFS (tốn ít memory).
- Do giới hạn thời gian chạy ngặt nghèo (<1000ms) và không gian tìm kiếm rất lớn (99 ô -> có thể lên tới `99!` nhánh), việc dùng BFS thuần sẽ gây tràn bộ nhớ (O(b^d)), dùng DFS thuần thì có thể sa lầy vào nhánh rất sâu.
- **Cách hoạt động:**
  1. Xác định một độ sâu `depthLimit` (bắt đầu bằng `lowerBound`).
  2. Dùng DFS để tìm tất cả các lời giải trong giới hạn `depthLimit`.
  3. Nếu tìm thấy ít nhất 1 lời giải, dừng tìm kiếm. Lời giải tìm được chắc chắn tốn ít move nhất.
  4. Nếu chưa tìm thấy, tăng `depthLimit` lên 1 và lặp lại từ bước 2.

## 4. Lower Bound (Cắt tỉa sớm - Pruning theo số bước tối thiểu)
- Mỗi lần match chỉ có thể thu được tối đa 2 Gem (nếu cả 2 ô đều là 5).
- Nếu ta còn thiếu `gemsRemaining`, số lượt đi tối thiểu cần thiết để thu đủ gem là: `lowerBound = (gemsRemaining + 1) / 2`.
- **Áp dụng:** Tại bất kỳ node nào trong cây DFS, nếu `movesUsed + lowerBound > depthLimit`, ta lập tức Prune (cắt tỉa) nhánh này vì không thể nào hoàn thành mục tiêu trong giới hạn `depthLimit` hiện tại.

## 5. Memoization (Transposition Table)
- **Cấu trúc:** `Dictionary<State, int> memo` lưu trạng thái của board (`State` mask) và số bước đi thấp nhất `bestMoves` để đạt được trạng thái đó.
- **Hoạt động:** Trước khi duyệt sâu hơn cho một trạng thái hiện tại, kiểm tra xem trạng thái đó đã từng được duyệt qua với số `movesUsed` ít hơn hoặc bằng chưa. Nếu có, ta prune luôn nhánh này vì nó không hiệu quả bằng đường đi trước đó.
- **Reset:** `memo` được reset lại mỗi khi `depthLimit` tăng lên trong IDDFS để tránh stale data.

## 6. Các kỹ thuật Pruning (Cắt tỉa khác)
- **Pruning 1 (Dead End Detection):** Kiểm tra xem số lượng Gem còn sót lại trên board (chưa bị match) cộng với số Gem đã thu thập có đủ để đạt `targetGems` không. Nếu không đủ, dù có đi hết bàn cũng thua -> Prune ngay.
- **Pruning 2 (Move Ordering):** Ở mỗi trạng thái, trước khi đệ quy DFS, sắp xếp các move hợp lệ ưu tiên move nào ăn được nhiều Gem nhất (`gems = 2` -> `gems = 1` -> `gems = 0`). Việc này giúp IDDFS nhanh chóng chạm được `targetGems` sớm nhất nếu nó nằm ở nhánh này.

## 7. Lấy Top 10 Lời Giải
- Thay vì dùng Beam Search sau khi IDDFS xong (vốn tốn memory và phức tạp), ta thu thập các lời giải trong đợt DFS hiện tại.
- Khi một lời giải chạm `targetGems` mà không vượt `depthLimit`, thay vì thoát hoàn toàn khỏi DFS, ta lưu vào `List<List<Move>> solutions` và `return` để DFS lùi lại (backtrack) và tiếp tục tìm nhánh khác ở CÙNG ĐỘ SÂU `depthLimit`.
- Khi đã đủ 10 lời giải (hoặc đã duyệt hết DFS cho độ sâu đó), ta mới ngắt IDDFS. Cách này đảm bảo 10 lời giải là các đường đi tối ưu nhất và memory hiệu quả hơn rất nhiều.

## 8. Hướng dẫn sử dụng Editor Tool
1. Mở file `Assets/StreamingAssets/input.txt` (nếu không có, Tool sẽ tự sinh ra file mẫu).
2. Điền tối đa 99 ký tự số (1-9) không dấu cách, không có ký tự ngắt dòng.
3. Trong Unity Editor, chọn menu **NumMatch -> Run Hard Mode Solver**.
4. Chờ Tool chạy (không cần vào Play Mode). Tool sẽ tự ngắt nếu quá giới hạn 900ms.
5. Mở thư mục `Assets/StreamingAssets/output.txt` để xem 10 lời giải (hoặc số lượng nhỏ hơn nếu không đủ lời giải). Console Unity sẽ in kết quả số node duyệt, số nhánh bị cắt, thời gian chạy.

## 9. Kết quả Benchmark (Tham khảo)
Dựa trên máy tính cá nhân cấu hình trung bình:
| Kích thước Input | Số lượng ô | Thời gian (ms) | Nhận xét |
| ---------------- | ---------- | -------------- | -------- |
| Nhỏ (2x9)        | 18         | ~5-15ms        | Rất nhanh, tìm mọi trường hợp chỉ trong chớp mắt |
| Vừa (5x9)        | 45         | ~50-150ms      | Phụ thuộc độ hên xui của Dead End, Pruning chặt hoạt động tốt |
| Lớn (11x9)       | 99         | <900ms         | Nhờ Timeout Threshold 900ms, Unity không bao giờ bị treo cứng. Trả về partial result nếu không kịp (do cấu hình máy) |
