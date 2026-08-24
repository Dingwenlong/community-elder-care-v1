import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:path/path.dart' as path;
import 'package:sqflite/sqflite.dart';

import 'outbox_entry.dart';

final outboxRepositoryProvider = Provider<OutboxRepository>((ref) {
  final repository = OutboxRepository();
  ref.onDispose(repository.close);
  return repository;
});

class OutboxRepository {
  OutboxRepository({String? databasePath}) : _configuredPath = databasePath;

  final String? _configuredPath;
  Future<Database>? _openingDatabase;
  Database? _database;

  Future<OutboxEntry> enqueue(OutboxEntry entry) async {
    final database = await _open();
    await database.insert(
      'outbox',
      entry.toDatabaseRow(),
      conflictAlgorithm: ConflictAlgorithm.ignore,
    );
    return await _findByRequestId(entry.requestId) ?? entry;
  }

  Future<List<OutboxEntry>> pending() async {
    final database = await _open();
    final rows = await database.query(
      'outbox',
      where: 'state IN (?, ?)',
      whereArgs: [OutboxState.pending.name, OutboxState.failed.name],
      orderBy: 'priority DESC, created_at ASC, id ASC',
    );
    return rows.map(OutboxEntry.fromDatabaseRow).toList(growable: false);
  }

  Future<OutboxEntry?> findByRequestId(String requestId) async {
    await _open();
    return _findByRequestId(requestId);
  }

  Future<void> markSent(int id) async {
    final database = await _open();
    await database.rawUpdate(
      '''
      UPDATE outbox
      SET state = ?, attempt_count = attempt_count + 1, last_error = NULL
      WHERE id = ?
      ''',
      [OutboxState.sent.name, id],
    );
  }

  Future<void> markFailed(int id, Object error) async {
    final database = await _open();
    await database.rawUpdate(
      '''
      UPDATE outbox
      SET state = ?, attempt_count = attempt_count + 1, last_error = ?
      WHERE id = ?
      ''',
      [OutboxState.failed.name, _safeError(error), id],
    );
  }

  Future<void> close() async {
    final database = _database;
    _database = null;
    _openingDatabase = null;
    if (database?.isOpen ?? false) await database!.close();
  }

  Future<Database> _open() {
    return _openingDatabase ??= _createDatabase();
  }

  Future<Database> _createDatabase() async {
    final databasePath =
        _configuredPath ??
        path.join(await getDatabasesPath(), 'community-care-outbox.db');
    final database = await openDatabase(
      databasePath,
      version: 1,
      onCreate: (database, version) async {
        await database.execute('''
          CREATE TABLE outbox (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            request_id TEXT NOT NULL UNIQUE,
            kind TEXT NOT NULL,
            payload_json TEXT NOT NULL,
            priority INTEGER NOT NULL,
            created_at TEXT NOT NULL,
            attempt_count INTEGER NOT NULL DEFAULT 0,
            last_error TEXT,
            state TEXT NOT NULL
          )
        ''');
        await database.execute('''
          CREATE INDEX IX_outbox_pending
          ON outbox(state, priority DESC, created_at, id)
        ''');
      },
    );
    _database = database;
    return database;
  }

  Future<OutboxEntry?> _findByRequestId(String requestId) async {
    final database = _database;
    if (database == null) return null;
    final rows = await database.query(
      'outbox',
      where: 'request_id = ?',
      whereArgs: [requestId],
      limit: 1,
    );
    if (rows.isEmpty) return null;
    return OutboxEntry.fromDatabaseRow(rows.single);
  }

  String _safeError(Object error) {
    final type = error.runtimeType.toString();
    return type.length <= 80 ? type : type.substring(0, 80);
  }
}
