# Numeria Battle Prototype

在 iPad Safari 里验证核心数学玩法的一次性原型:咒语算式拖水晶、凑十破盾、能量宝石、零惩罚重试、英文语音旁白。

## 运行

```bash
cd prototype && python3 -m http.server 8765
```

Mac 与 iPad 同一 Wi-Fi 下,iPad Safari 访问 `http://<Mac的IP>:8765`。

## 测试

```bash
npm test   # 仓库根目录,Node ≥ 18
```
