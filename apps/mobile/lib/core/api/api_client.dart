import 'dart:convert';

import 'package:http/http.dart' as http;

import 'api_problem.dart';

typedef JsonDecoder<T> = T Function(Object? json);

class ApiClient {
  ApiClient({
    required this.baseUri,
    http.Client? httpClient,
    this.onUnauthorized,
  }) : _httpClient = httpClient ?? http.Client();

  final Uri baseUri;
  final http.Client _httpClient;
  final void Function()? onUnauthorized;
  String? _accessToken;

  void setAccessToken(String? token) => _accessToken = token;

  Future<T> get<T>(String path, JsonDecoder<T> decode) =>
      _request('GET', path, decode: decode);

  Future<T> post<T>(String path, JsonDecoder<T> decode, {Object? body}) =>
      _request('POST', path, decode: decode, body: body);

  Future<T> put<T>(String path, JsonDecoder<T> decode, {Object? body}) =>
      _request('PUT', path, decode: decode, body: body);

  Future<T> delete<T>(String path, JsonDecoder<T> decode, {Object? body}) =>
      _request('DELETE', path, decode: decode, body: body);

  Future<T> _request<T>(
    String method,
    String path, {
    required JsonDecoder<T> decode,
    Object? body,
  }) async {
    final headers = <String, String>{'Accept': 'application/json'};
    if (_accessToken != null) headers['Authorization'] = 'Bearer $_accessToken';
    if (body != null) headers['Content-Type'] = 'application/json';
    final request = http.Request(method, baseUri.resolve(path))
      ..headers.addAll(headers);
    if (body != null) request.body = jsonEncode(body);

    final streamedResponse = await _httpClient.send(request);
    final response = await http.Response.fromStream(streamedResponse);
    if (response.statusCode == 401) onUnauthorized?.call();
    final Object? json = response.body.isEmpty
        ? null
        : jsonDecode(response.body);
    if (response.statusCode < 200 || response.statusCode >= 300) {
      final problemJson = json is Map<String, dynamic>
          ? Map<String, Object?>.from(json)
          : <String, Object?>{};
      throw ApiProblem.fromJson(response.statusCode, problemJson);
    }
    return decode(json);
  }

  void close() => _httpClient.close();
}
