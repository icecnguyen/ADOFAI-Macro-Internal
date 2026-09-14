# ADOFAI Macro Internal

<p align="center">
  <b>
  A high-performance, internal autoplay & macro mod for A Dance of Fire and Ice (ADOFAI) via UnityModManager.
  </b>
</p>

<p align="center">
  <a href="#english">
  English
  </a> |
  <a href="#tiếng-việt">
  Tiếng Việt
  </a>
</p>

---

<a name="english"></a>
## English

### Overview
**ADOFAI Macro Internal** is a high-performance internal macro mod for **A Dance of Fire and Ice**, integrated directly into the game via **UnityModManager**. It features low-latency frame-accurate input timing, smart hand and finger stream allocation based on <a href="https://github.com/CLiF-1593/ADOFAI_Macro_for_Publish.git">ADOFAI_Macro_for_Publish</a>, and customizable key limit / roll styles.

### Features
- **Pure Perfect Frame-Centering**: Synchronized directly with Conductor (`songposition_minusi`), compensating for frame quantization latency (`frameLead = dt * 0.5 + 0.0015s`) to hit Pure Perfect consistently.
- **Accurate Hold Event Support**: Automatically identifies hold notes, keeps keys held, and releases them at the exact release time using a dedicated low-latency background thread (`FastKeyReleaser`).
- **5 Roll Styles**:
  - `Inward`: Outer-to-inner roll.
  - `Outward`: Inner-to-outer roll.
  - `Staircase`: Stepwise roll according to hand.
  - `Reverse Staircase`: Inverted stepwise roll.
  - `Random`: Automatically switches roll styles at customizable time intervals (3s - 30s) or natural music pauses, with a **Refresh** button to re-roll immediately.
- **Key Limit System**:
  - Solves note congestion and over-stacking (e.g. 8K midspins are evenly divided into **4-4** instead of 8-8 on one hand).
  - Quick presets: `None` (unconstrained), `2K`, `4K`, `6K`, `8K`, `10K`, `12K`, `14K`, `16K`, `32K`.
  - Custom input: Set any number of keys per hand (`0` = None, `1` to `16`).
- **Main Hand Selection**: Choose **Left Hand** or **Right Hand** as the primary hand to initiate bursts.
- **Human Spoof**: Optional Gaussian jitter (ms) to emulate human-like micro-deviations.
- **32-Key Visual Keybinding**: 16 keys for Left Hand, 16 keys for Right Hand with direct key capture in the UMM menu.
- **Bilingual Interface**: Seamless switching between Vietnamese and English.
- **Non-blocking Background JSON Cache**: Level data, note hands, fingers, and keycodes are exported asynchronously to `Cache/` without causing frame drops.

### Installation
1. Install **UnityModManager (UMM)** for *A Dance of Fire and Ice*.
2. Copy `Info.json` and `ADOFAI-Macro-Internal.dll` into:
   ```
   <Steam-Library>/steamapps/common/A Dance of Fire and Ice/Mods/ADOFAIMacroInternal/
   ```
3. Launch the game and open the UMM menu (default `Ctrl + F10`) to configure keys and settings.

### Building from Source
**Prerequisites:** [.NET SDK](https://dotnet.microsoft.com/download)
```bash
git clone https://github.com/icecnguyen/ADOFAI-Macro-Internal.git
cd ADOFAI-Macro-Internal

dotnet build -c Release
```

---

<a name="tiếng-việt"></a>
## Tiếng Việt

### Giới thiệu
**ADOFAI Macro Internal** là mod macro hiệu năng cao dành cho tựa game **A Dance of Fire and Ice**, hoạt động trực tiếp trong game thông qua **UnityModManager**. Mod tối ưu hóa độ trễ cực thấp, căn nhịp theo Conductor của game và phân bổ luồng phím thông minh dựa trên thuật toán chuẩn của <a href="https://github.com/CLiF-1593/ADOFAI_Macro_for_Publish.git">ADOFAI_Macro_for_Publish</a>.

### Tính năng nổi bật
- **Căn nhịp Pure Perfect**: Đồng bộ hóa trực tiếp theo Conductor (`songposition_minusi`), bù trừ độ trễ lượng tử hóa khung hình (`frameLead = dt * 0.5 + 0.0015s`) đạt độ chính xác tuyệt đối.
- **Xử lý Hold Note (Nốt giữ)**: Tự động nhận diện, đè giữ và nhả phím đúng thời điểm qua luồng ngắt nhả phím nền tốc độ cao (`FastKeyReleaser`).
- **5 kiểu Roll đa dạng**:
  - `Inward`: Roll từ ngoài vào trong.
  - `Outward`: Roll từ trong ra ngoài.
  - `Staircase`: Đánh kiểu bậc thang theo tay.
  - `Reverse Staircase`: Đánh kiểu bậc thang ngược.
  - `Ngẫu nhiên`: Tự động đảo kiểu roll sau mỗi khoảng thời gian tùy chỉnh (3s - 30s) hoặc tại các đoạn nghỉ tự nhiên, có nút **Làm mới** để re-roll map ngay lập tức.
- **Hệ thống Giới hạn phím**:
  - Giải quyết triệt để vấn đề dồn nốt quá tải.
  - Tùy chọn sẵn: `Không giới hạn`, `2 phím`, `4 phím`, `6 phím`, `8 phím`, `10 phím`, `12 phím`, `14 phím`, `16 phím`, `32 phím`.
  - Ô nhập tùy chỉnh: Cho phép đặt bất kỳ số lượng phím mong muốn từ `0` đến `16` phím mỗi tay.
- **Chọn tay chính**: Chọn **Tay trái** hoặc **Tay phải** làm tay ưu tiên đánh nốt khởi đầu.
- **Giả lập con người**: Tùy chọn tạo sai số ngẫu nhiên theo phân phối chuẩn Gaussian giúp hành vi gõ tự nhiên như người thật.
- **Gán phím 32 phím trực quan**: 16 phím cho Tay Trái, 16 phím cho Tay Phải, bấm gán trực tiếp trên menu UMM.
- **Giao diện Song ngữ**: Chuyển đổi linh hoạt giữa Tiếng Việt và English.
- **Ghi Cache nền thông minh**: Xuất dữ liệu nốt ra file JSON trong thư mục `Cache/` không gây giật lag game.

### Cài đặt
1. Cài đặt **UnityModManager** cho game *A Dance of Fire and Ice*.
2. Sao chép `Info.json` và `ADOFAI-Macro-Internal.dll` vào:
   ```
   <Thư mục Steam>/steamapps/common/A Dance of Fire and Ice/Mods/ADOFAIMacroInternal/
   ```
3. Mở game, nhấn phím tắt mở menu UMM (mặc định `Ctrl + F10`) để cài đặt phím và chế độ đánh.

### Biên dịch từ mã nguồn
**Yêu cầu:** [.NET SDK](https://dotnet.microsoft.com/download)
```bash
git clone https://github.com/icecnguyen/ADOFAI-Macro-Internal.git
cd ADOFAI-Macro-Internal

dotnet build -c Release
```

---

## Project Structure / Cấu trúc dự án

```
ADOFAI-Macro-Internal/
├── AdoMacro3Engine.cs
├── InputSimulator.cs
├── Patches.cs
├── Settings.cs
├── Main.cs
├── Info.json
├── LICENSE
└── README.MD
```

---

## Credits & Acknowledgments
- **[ADOFAI_Macro_for_Publish](https://github.com/CLiF-1593/ADOFAI_Macro_for_Publish)**: Original macro algorithm concepts, hand/finger allocation logic, and roll techniques.
- **[icecnguyen](https://github.com/icecnguyen)**: Internal C# architecture porting, dynamic Key Limit system, hold event synchronization, bilingual UMM GUI, and ongoing maintenance.

---

## License
This project is licensed under the **GNU General Public License v2.0 (GPL-2.0)** - see the [LICENSE](LICENSE) file for details.

*Disclaimer: This project is intended for educational, technical research, and offline entertainment purposes. Please do not use this mod to cheat on official public leaderboards.*