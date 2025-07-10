#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🔄 Bot Manager Watchdog - Демон для автоматического перезапуска
Отслеживает изменения в bot_manager.py и перезапускает процесс
"""

import os
import sys
import time
import subprocess
import logging
from pathlib import Path
from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler

class BotManagerHandler(FileSystemEventHandler):
    """Обработчик событий файловой системы"""
    
    def __init__(self):
        self.process = None
        self.restart_needed = False
        
    def on_modified(self, event):
        if event.is_directory:
            return
            
        # Отслеживаем только Python файлы
        if event.src_path.endswith('.py'):
            file_name = os.path.basename(event.src_path)
            logging.info(f"🔄 Обнаружено изменение файла: {file_name}")
            self.restart_needed = True
    
    def start_bot_manager(self):
        """Запускает бот менеджер"""
        try:
            if self.process:
                self.stop_bot_manager()
            
            logging.info("🚀 Запуск Bot Manager...")
            self.process = subprocess.Popen(
                [sys.executable, 'bot_manager.py'],
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                cwd=os.path.dirname(__file__)
            )
            logging.info(f"✅ Bot Manager запущен (PID: {self.process.pid})")
            
        except Exception as e:
            logging.error(f"❌ Ошибка запуска Bot Manager: {e}")
    
    def stop_bot_manager(self):
        """Останавливает бот менеджер"""
        if self.process:
            try:
                self.process.terminate()
                self.process.wait(timeout=10)
                logging.info("🛑 Bot Manager остановлен")
            except subprocess.TimeoutExpired:
                self.process.kill()
                logging.warning("⚡ Bot Manager принудительно завершен")
            except Exception as e:
                logging.error(f"❌ Ошибка остановки Bot Manager: {e}")
    
    def restart_bot_manager(self):
        """Перезапускает бот менеджер"""
        logging.info("🔄 Перезапуск Bot Manager...")
        self.stop_bot_manager()
        time.sleep(2)  # Пауза перед перезапуском
        self.start_bot_manager()
        self.restart_needed = False

def main():
    """Главная функция watchdog"""
    print("""
    ╔════════════════════════════════════════════════════════════════════════════════════════╗
    ║                      🔄 Bot Manager Watchdog v1.0                                     ║
    ║                     Автоматический перезапуск при изменениях                           ║
    ╠════════════════════════════════════════════════════════════════════════════════════════╣
    ║ Отслеживает изменения в Python файлах и автоматически перезапускает Bot Manager       ║
    ║                                                                                        ║
    ║ 🔍 Отслеживаемые файлы: *.py                                                           ║
    ║ 🔄 Автоматический перезапуск: Да                                                       ║
    ║ 📁 Директория: текущая папка                                                           ║
    ╚════════════════════════════════════════════════════════════════════════════════════════╝
    """)
    
    # Настройка логирования
    logging.basicConfig(
        level=logging.INFO,
        format='%(asctime)s - %(levelname)s - %(message)s',
        handlers=[
            logging.FileHandler('watchdog.log', encoding='utf-8'),
            logging.StreamHandler()
        ]
    )
    
    # Создаем обработчик и наблюдатель
    event_handler = BotManagerHandler()
    observer = Observer()
    
    # Отслеживаем текущую директорию
    watch_path = os.path.dirname(__file__) or '.'
    observer.schedule(event_handler, watch_path, recursive=False)
    
    try:
        # Запускаем Bot Manager в первый раз
        event_handler.start_bot_manager()
        
        # Запускаем наблюдатель
        observer.start()
        logging.info(f"🔍 Начато отслеживание изменений в: {os.path.abspath(watch_path)}")
        
        # Основной цикл
        while True:
            if event_handler.restart_needed:
                event_handler.restart_bot_manager()
            
            # Проверяем, жив ли процесс
            if event_handler.process and event_handler.process.poll() is not None:
                logging.warning("⚠️ Bot Manager завершился, перезапускаем...")
                event_handler.start_bot_manager()
            
            time.sleep(1)
            
    except KeyboardInterrupt:
        logging.info("⚠️ Получен сигнал остановки...")
        observer.stop()
        event_handler.stop_bot_manager()
        
    observer.join()
    logging.info("✅ Watchdog завершен")

if __name__ == "__main__":
    main() 