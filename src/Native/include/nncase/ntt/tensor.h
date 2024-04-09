/* Copyright 2019-2021 Canaan Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#pragma once
#include "detail/shape_storage.h"
#include "detail/tensor_storage.h"

namespace nncase::ntt {
template <class T, class Shape, class Strides, size_t MaxSize, bool IsView>
class tensor_base;

template <class T, class Shape, class Strides = default_strides_t<Shape>,
          size_t MaxSize = max_size_v<Shape, Strides>>
using tensor = tensor_base<T, Shape, Strides, MaxSize, false>;

template <class T, class Shape, class Strides = default_strides_t<Shape>,
          size_t MaxSize = max_size_v<Shape, Strides>>
using tensor_view = tensor_base<T, Shape, Strides, MaxSize, true>;

template <class T, size_t... Lanes>
using fixed_tensor = tensor<T, fixed_shape<Lanes...>>;

template <class T, size_t... Lanes> using vector = fixed_tensor<T, Lanes...>;

namespace detail {
template <class T, class Shape, class Strides, size_t MaxSize, bool IsView,
          bool IsFixedShape = is_fixed_dims_v<Shape> &&is_fixed_dims_v<Strides>>
class tensor_impl;

// dynamic tensor
template <class T, class Shape, class Strides, size_t MaxSize>
class tensor_impl<T, Shape, Strides, MaxSize, false, false>
    : public detail::tensor_storage<T, MaxSize, false>,
      public detail::tensor_size_impl<Shape, Strides> {
    using size_impl_type = detail::tensor_size_impl<Shape, Strides>;

  public:
    using element_type = T;
    using storage_type = detail::tensor_storage<T, MaxSize, false>;

    tensor_impl(Shape shape, Strides strides)
        : storage_type(linear_size(shape, strides)),
          size_impl_type(shape, strides) {}
    tensor_impl(Shape shape) : tensor_impl(shape, default_strides(shape)) {}
};

// fixed tensor
template <class T, class Shape, class Strides, size_t MaxSize>
class tensor_impl<T, Shape, Strides, MaxSize, false, true>
    : public detail::tensor_storage<T, MaxSize, false>,
      public detail::tensor_size_impl<Shape, Strides> {
    using size_impl_type = detail::tensor_size_impl<Shape, Strides>;

  public:
    using element_type = T;
    using storage_type = detail::tensor_storage<T, MaxSize, false>;
    using buffer_type = typename storage_type::buffer_type;

    tensor_impl(Shape = {}, Strides = {}) noexcept {}
    tensor_impl(buffer_type buffer) noexcept
        : storage_type(std::in_place, std::move(buffer)) {}

    explicit tensor_impl(T value) noexcept;
};

// dynamic view
template <class T, class Shape, class Strides, size_t MaxSize>
class tensor_impl<T, Shape, Strides, MaxSize, true, false>
    : public detail::tensor_storage<T, MaxSize, true>,
      public detail::tensor_size_impl<Shape, Strides> {
    using size_impl_type = detail::tensor_size_impl<Shape, Strides>;

  public:
    using storage_type = detail::tensor_storage<T, MaxSize, true>;
    using buffer_type = typename storage_type::buffer_type;

    tensor_impl(buffer_type buffer, Shape shape, Strides strides)
        : storage_type(std::in_place, std::move(buffer)),
          size_impl_type(shape, strides) {}
    tensor_impl(buffer_type buffer, Shape shape)
        : tensor_impl(std::move(buffer), shape, default_strides(shape)) {}
};

// fixed view
template <class T, class Shape, class Strides, size_t MaxSize>
class tensor_impl<T, Shape, Strides, MaxSize, true, true>
    : public detail::tensor_storage<T, MaxSize, true>,
      public detail::tensor_size_impl<Shape, Strides> {
    using size_impl_type = detail::tensor_size_impl<Shape, Strides>;

  public:
    using element_type = T;
    using storage_type = detail::tensor_storage<T, MaxSize, true>;
    using buffer_type = typename storage_type::buffer_type;

    tensor_impl(buffer_type buffer, Shape = {}, Strides = {}) noexcept
        : storage_type(std::in_place, std::move(buffer)) {}
};
} // namespace detail

template <class T, class Shape, class Strides, size_t MaxSize, bool IsView>
class tensor_base
    : public detail::tensor_impl<T, Shape, Strides, MaxSize, IsView> {
    using impl_type = detail::tensor_impl<T, Shape, Strides, MaxSize, IsView>;
    using size_impl_type = detail::tensor_size_impl<Shape, Strides>;

  public:
    using element_type = T;
    using storage_type = detail::tensor_storage<T, MaxSize, IsView>;
    using buffer_type = typename storage_type::buffer_type;
    using shape_type = Shape;
    using strides_type = Strides;

    using size_impl_type::shape;
    using size_impl_type::size;
    using size_impl_type::strides;
    using storage_type::buffer;
    using storage_type::elements;

    using impl_type::impl_type;

    operator const buffer_type &() const noexcept { return buffer(); }
    operator buffer_type &() noexcept { return buffer(); }

    static tensor_base<T, Shape, Strides, MaxSize, IsView> from_scalar(T value);

    template <class Index, class UShape>
    constexpr tensor_view<T, UShape, Strides> view(Index index,
                                                   UShape shape) noexcept {
        if constexpr (is_fixed_dims_v<Strides>) {
            auto offset = linear_offset(index, strides());
            auto begin = elements().data() + offset;
            if constexpr (is_fixed_dims_v<UShape>) {
                constexpr size_t size = linear_size(shape, strides());
                return {std::span<T, size>(begin, size), shape, strides()};
            } else {
                size_t size = linear_size(shape, strides());
                return {std::span(begin, size), shape, strides()};
            }
        } else {
            return {elements().subspan(linear_offset(index, strides()),
                                       linear_size(shape, strides())),
                    shape, strides()};
        }
    }

    template <typename TNewShape>
    constexpr tensor_view<T, TNewShape, default_strides_t<TNewShape>>
    reshape(TNewShape shape) noexcept {
        return {buffer(), shape, default_strides(shape)};
    }

    constexpr tensor_view<T, Shape, Strides> view() noexcept {
        return view(zero_shape_t<Shape::rank()>{}, shape());
    }

    template <class... Indices>
    constexpr const T &operator()(Indices &&...index) const noexcept {
        if constexpr (sizeof...(index) == 1 &&
                      (!std::is_integral_v<Indices> && ...)) {
            return elements()[linear_offset(index..., strides())];
        } else {
            return this->operator()(
                ranked_shape<sizeof...(index)>{static_cast<size_t>(index)...});
        }
    }

    template <class... Indices>
    constexpr T &operator()(Indices &&...index) noexcept {
        if constexpr (sizeof...(index) == 1 &&
                      (!std::is_integral_v<Indices> && ...)) {
            return elements()[linear_offset(index..., strides())];
        } else {
            return this->operator()(
                ranked_shape<sizeof...(index)>{static_cast<size_t>(index)...});
        }
    }
};
} // namespace nncase::ntt