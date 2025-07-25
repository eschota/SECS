# Обновление таймаута матчей

## Изменение

**Файл:** `Server/Models/GameMatch.cs`  
**Свойство:** `MatchMaxTimeLimit`

**Было:** `60.0f` (1 минута)  
**Стало:** `600.0f` (10 минут)

## Описание

Увеличен таймаут завершения матчей с 1 минуты до 10 минут для всех типов игр:
- 1v1 (OneVsOne)
- 2v2 (TwoVsTwo) 
- 4-player FFA (FourPlayerFFA)

## Логика работы

1. **Создание матча:** Когда создается новый матч, устанавливается `StartTime = DateTime.UtcNow`

2. **Проверка истечения:** Каждые 10 секунд `MatchmakingService` проверяет активные матчи через свойство `IsExpired`:
   ```csharp
   public bool IsExpired => DateTime.UtcNow > StartTime.AddSeconds(MatchMaxTimeLimit);
   ```

3. **Автозавершение:** Если матч истек:
   - Статус меняется на `Completed`
   - Устанавливается `EndTime = DateTime.UtcNow`
   - Выбирается случайный победитель из участников
   - Обновляется MMR всех игроков (+20 победителю, -20 проигравшим)
   - Логируется: `"Match {MatchId} timed out, random winner: {winnerId}"`

## Влияние на игру

- ✅ Больше времени для завершения реальных матчей
- ✅ Снижение количества случайных победителей по таймауту
- ✅ Более комфортный игровой процесс
- ✅ Сохраняется защита от зависших матчей

## Тестирование

После перезапуска сервера новые матчи будут использовать таймаут 10 минут. Проверить можно:

1. Создать тестовый матч
2. Ждать 10 минут без активности
3. Убедиться что матч автоматически завершился с сообщением "timed out"

**Примечание:** Изменение применяется только к новым матчам. Текущие активные матчи продолжат использовать старый таймаут до своего завершения. 