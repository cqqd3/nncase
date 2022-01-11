# Copyright 2019-2021 Canaan Inc.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
# pylint: disable=invalid-name, unused-argument, import-outside-toplevel

import pytest
import onnx
from onnx import helper
from onnx import AttributeProto, TensorProto, GraphProto
from onnx_test_runner import OnnxTestRunner


def _make_module(in_shape):
    x = helper.make_tensor_value_info('x', TensorProto.FLOAT, in_shape)
    y = helper.make_tensor_value_info('y', TensorProto.FLOAT, in_shape)
    z = helper.make_tensor_value_info('z', TensorProto.FLOAT, in_shape)

    add = onnx.helper.make_node(
        'Add',
        inputs=['x', 'y'],
        outputs=['sum'],
    )

    identity = onnx.helper.make_node(
        'Identity',
        inputs=['sum'],
        outputs=['z'],
    )

    graph_def = helper.make_graph(
        [add, identity],
        'test-model',
        [x, y],
        [z]
    )

    model_def = helper.make_model(graph_def, producer_name='kendryte')

    return model_def


in_shapes = [
    [1, 3, 48, 48]
]


@pytest.mark.parametrize('in_shape', in_shapes)
def test_identity(in_shape, request):
    model_def = _make_module(in_shape)

    runner = OnnxTestRunner(request.node.name)
    model_file = runner.from_onnx_helper(model_def)
    runner.run(model_file)


if __name__ == "__main__":
    pytest.main(['-vv', 'test_identity.py'])
