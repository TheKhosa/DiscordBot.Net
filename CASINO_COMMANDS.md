# Casino Commands Quick Reference

## 🎰 Roulette
**Place Bet:** `!roulette <type> <amount>` or `!roul <type> <amount>`  
**Spin:** `!spin`

**Bet Types:**
```
red, black     → 2x payout
odd, even      → 2x payout  
low, high      → 2x payout (1-18 / 19-36)
1-12, 13-24, 25-36 → 3x payout (dozens)
0-36 (number)  → 36x payout
```

**Examples:**
```
!roulette red 100
!spin

!roulette 17 50
!spin
```

---

## 🎲 Craps (Dice Game)
**Start:** `!craps 500`  
**Roll:** `!roll`

**Rules:**
- **Come-out roll:**
  - 7 or 11 = Win (natural)
  - 2, 3, or 12 = Lose (craps)
  - Any other = Point established
- **Point rolls:**
  - Roll the point = Win
  - Roll 7 = Lose (seven out)
  - Keep rolling otherwise

---

## 🎲 High-Low (Card Guessing)
**Start:** `!highlow 100` or `!hl 100`  
**Play:** `!higher` or `!hi` | `!lower` or `!lo`  
**Cash Out:** `!cashout`

**Streak Multipliers:**
```
1 win  → 1.8x    |  6 wins → 10x
2 wins → 2.5x    |  7 wins → 15x
3 wins → 3.5x    |  8 wins → 25x
4 wins → 5x      |  9+ wins → 50x
5 wins → 7x      |
```

---

## ⚔️ War (Instant Battle)
**Play:** `!war 500`

- Draw vs dealer, highest card wins
- Ties return your bet
- Wins pay 2x your bet (1:1)

---

## 🎴 Blackjack (21)
**Start:** `!blackjack 500` or `!bj 500`  
**Actions:** `!hit` | `!stand` | `!doubledown` (or `!dd`)

**Payouts:**
- Blackjack: 1.5x bet
- Win: 2x bet
- Push: Bet returned
- Lose: Bet lost

---

## 🃏 Poker (Texas Hold'em)
**Create Table:** `!poker 1000`  
**Join:** `!joinpoker <game-id>`  
**Start:** `!startpoker` (2+ players)  
**Actions:** `!call` | `!raise 100` | `!fold` | `!check`  
**Close:** `!leavetable`

---

## 💰 Economy Integration
All games use coins from the Economy module:

- `!balance` - Check your coins
- `!daily` - Daily reward
- `!work` - Earn coins
- `!givemoney @user 1000` - Admin: Give coins

---

## 🎰 Pro Tips

**High-Low Strategy:**
- Early cashout (3-4 wins) = steady profits
- Risk it for 7+ wins = massive payouts
- Watch for 10s and face cards!

**War Strategy:**
- Pure luck - quick fun game
- Good for fast coin doubling

**Blackjack Strategy:**
- Dealer hits to 17
- Double down on 10 or 11
- Never hit on 17+

**Poker Strategy:**
- Position matters
- Fold weak hands early
- Raise to build the pot with strong hands

---

## 📊 Deck Management
✅ Each game gets a **brand new shuffled deck**  
✅ Decks are **never reused** between games  
✅ API ensures **true randomness** from deckofcardsapi.com  
✅ Console logs show deck IDs for verification

---

## 🆘 Troubleshooting

**"You already have an active game"**
→ Finish your current game first

**"Not enough coins"**
→ Use `!daily`, `!work`, or ask admin for `!givemoney`

**"Failed to create deck"**
→ Retry the command (API connection issue)

**Stuck in poker table?**
→ Use `!leavetable` to close channel
