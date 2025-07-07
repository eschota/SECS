# match_endpoints.py

#Техническое задание:
# создать endpoint для работы с матчами
# матч - это игра между двумя игроками или более игроками.
# class Match:
#     match_type: str - тип матча:
#         - 1v1
#         - 2v2
#         - 3v3
#         - 4v4
#         - 5v5
#         - 6v6
#     match_id: int - порядковый номер матча, уникальный идентификатор - auto increment
#     players: list[str] - список игроков в матче
#     start_time: datetime - время начала матча
#     end_time: datetime - время окончания матча
#     status: str
#     game_actions: list[dict]
#     result_win: list[user_id] - список игроков, которые выиграли матч
#     result_lose: list[user_id] - список игроков, которые проиграли матч
#     result_surrender: list[user_id] - список игроков, которые сдались
#     result_draw: list[user_id] - список игроков, которые сыграли вничью
#     result_cancel: list[user_id] - список игроков, которые отменили матч
# class GameAction:
#     action_id: int - порядковый номер действия, уникальный идентификатор - auto increment
#     player_id: int - id игрока
#     action_type: str - тип действия:

# Matches:
# matches_active: list[Match] - список активных матчей
# matches_history: list[Match] - список завершенных матчей

#







from flask import Blueprint, request, jsonify
from database import db
from datetime import datetime, timedelta
from dataclasses import dataclass, field
from typing import List, Any
import uuid

match_bp = Blueprint('match', __name__)


