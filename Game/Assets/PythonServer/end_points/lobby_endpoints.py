from flask import Blueprint, request, jsonify
from database import db

lobby_bp = Blueprint('lobby', __name__)
