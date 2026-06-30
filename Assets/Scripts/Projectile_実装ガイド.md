# Projectile.cs 実装ガイド（初心者向け）

`Projectile.cs` は**発射体（弾）**のスクリプトです。今はフィールドだけある「スケルトン（骨組み）」状態なので、
このガイドに沿って `Start` と `OnTriggerEnter` の中身を書いてください。

---

## 0. まず全体像

- 弾は `PlayerAttackController`（以下PAC）が `Instantiate` で生成します。
- 生成直後、PACが弾の **public フィールドに値を入れて**くれます。だからあなたは「値はもう入っている」前提で使えます。

PACが入れてくれるフィールド（`Projectile.cs` に既にあります）:

| フィールド | 型 | 中身 | どこから来る |
|------------|----|------|--------------|
| `damage`    | int     | 与えるダメージ | PACが発射時に代入 |
| `speed`     | float   | 飛ぶ速さ       | PACが発射時に代入 |
| `range`     | float   | 飛距離         | PACが発射時に代入 |
| `ownerId`   | int     | 撃った人の番号 | PACが発射時に代入 |
| `ownerTeam` | int     | 撃った人のチーム | PACが発射時に代入 |
| `direction` | Vector3 | 飛ぶ向き       | PACが発射時に代入 |

---

## 1. Unity側の準備（Prefab作成）

弾のPrefabには次の3つが必要です:

1. **Rigidbody**（物理で動かすため）
   - インスペクターで `Use Gravity` の**チェックを外す**（重力で落ちないように）。
2. **Collider**（当たり判定。SphereColliderなどでOK）
   - `Is Trigger` の**チェックを入れる**。これが入っていないと `OnTriggerEnter` が呼ばれません。
3. **Projectile.cs**（このスクリプト）

> プレイヤー側には「Is Trigger オフのCollider」と「Rigidbody」が付いている前提です。
> 片方がTrigger・両方にColliderがあると `OnTriggerEnter` が成立します。

---

## 2. `Start` でやること（動かす＋寿命管理）

弾が生まれた瞬間に、**進む向きに speed の速さで飛ばし**、`range / speed` 秒たったら自動で消します。

必要なローカル変数:
- `Rigidbody rb` … この弾のRigidbodyを入れておく変数。

手順:
1. `GetComponent<Rigidbody>()` で自分のRigidbodyを取り、`rb` に入れる。
2. `direction` はもしかすると長さが1でないので `direction.normalized`（長さ1の向き）にして、`speed` を掛けると「速度ベクトル」になる。
   - 速度ベクトル = `direction.normalized * speed`
3. それを `rb.linearVelocity` に代入すると、その向き・速さで飛び続ける。
   - （Unity 6では速度は `linearVelocity`。古い記事の `velocity` と同じ意味です）
4. `Destroy(gameObject, range / speed)` を書く。
   - 「`range`（飛距離）÷`speed`（速さ）」＝「飛びきるのにかかる秒数」。その秒数後に弾を消す、という意味。

イメージ（穴埋め）:
```csharp
void Start()
{
    Rigidbody rb = GetComponent<Rigidbody>();
    rb.linearVelocity = direction.normalized * speed;
    Destroy(gameObject, range / speed);
}
```

---

## 3. `OnTriggerEnter` でやること（当たった時の処理）

弾が何かに触れると `OnTriggerEnter(Collider other)` が呼ばれます。`other` が「触れた相手」です。

ルール:
- **自分**（撃った本人）には当てない → 何もせず素通り（弾は消さない）。
- **味方**（同じチーム）には → ダメージ無し。でも弾は消す。
- **敵**には → ダメージを与えて、弾も消す。
- プレイヤー以外（壁など）に当たった場合は → 今回は無視（何もしない）。

必要なローカル変数:
- `PlayerHPManager hp` … 当たった相手のHP管理スクリプト。相手がプレイヤーなら入る、そうでなければ `null`。

「相手がプレイヤーか」「自分/味方/敵か」を判定するための情報は、相手の `PlayerHPManager` が公開しています:
- `hp.PlayerId` … 相手の番号 → 自分の `ownerId` と比べる。
- `hp.Team` … 相手のチーム → 自分の `ownerTeam` と比べる。
- `hp.TakeDamage(damage)` … 相手にダメージを与えるメソッド。

手順:
1. `other.GetComponent<PlayerHPManager>()` を取り、`hp` に入れる。
2. `if (hp == null) return;` … 相手がプレイヤーでなければ何もしない。
3. `if (hp.PlayerId == ownerId) return;` … 撃った本人なら素通り（弾は残す）。
4. `if (hp.Team == ownerTeam) { Destroy(gameObject); return; }` … 味方ならダメージ無しで弾だけ消す。
5. ここまで来たら相手は敵。`hp.TakeDamage(damage);` でダメージを与え、`Destroy(gameObject);` で弾を消す。

イメージ（穴埋め）:
```csharp
void OnTriggerEnter(Collider other)
{
    PlayerHPManager hp = other.GetComponent<PlayerHPManager>();
    if (hp == null) return;                 // プレイヤー以外は無視
    if (hp.PlayerId == ownerId) return;     // 自分には当たらない
    if (hp.Team == ownerTeam)               // 味方
    {
        Destroy(gameObject);                // ダメージ無しで弾だけ消す
        return;
    }
    hp.TakeDamage(damage);                  // 敵にダメージ
    Destroy(gameObject);                    // 弾を消す
}
```

---

## 4. 確認のしかた

1. プレイヤーを動かして A/B ボタンで弾を撃つ。
2. 弾がまっすぐ飛び、しばらくして消える（= `Start` のDestroyが効いている）。
3. 敵に当てると Console に `Player N がダメージ … HP …` が出る（= `TakeDamage` が呼べている）。
4. 味方に当てるとダメージログ無しで弾だけ消える。自分には当たらず素通り。

うまく動かない時のチェック:
- 弾のColliderの `Is Trigger` が ON か。
- 弾とプレイヤーの両方に Collider があるか。少なくとも片方に Rigidbody があるか。
- `OnTriggerEnter` のスペルが合っているか（`OnTriger…` などの打ち間違いに注意）。
