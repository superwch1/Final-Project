#pragma once
#include <vector>
#include <functional>

template <typename T>

class Event {
  std::vector<std::function<void(T)>> _listeners;   

public:
  /// Add a listener 
  void subscribe(std::function<void(T)> listener) {
    _listeners.push_back(listener);
  }

  /// Call every listener with the argument
  void emit(T arg) {
    for (auto& listener : _listeners) {       
      listener(arg);
    }
  }
};